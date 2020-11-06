﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  //Corresponds to LOAD_GRID_AREA
  public static class Structural2DLoadPanelToNative
  {
    private static readonly LoadDirection3[] loadDirSeq = new LoadDirection3[] { LoadDirection3.X, LoadDirection3.Y, LoadDirection3.Z };

    public static string ToNative(this Structural2DLoadPanel loadPanel)
    {
      if (loadPanel.Loading == null || loadPanel.Loading.Value == null || loadPanel.Loading.Value.All(v => v == 0))
      {
        return "";
      }

      var keyword = GsaRecord.Keyword<GsaLoadGridArea>();

      var loadCaseKeyword = GsaRecord.Keyword<GsaLoadCase>();
      var loadCaseIndex = Initialiser.Cache.ResolveIndex(loadCaseKeyword, loadPanel.LoadCaseRef);

      var gwaList = new List<string>();
      var loadingDict = ExplodeLoading(loadPanel.Loading);
      var originalPolyline = loadPanel.Value.ToArray();

      //There are two possible axes at play here:
      //1.  one associated with the grid surface (referred by LoadPlaneRef) applied to the coordinates of the polyline
      //2.  one associated with the loading - i.e. applied to the load
      //Note: only the first is supported here

      //When retrieving the axis (to use in transforming the polyline etc), there are two routes here:
      //1.  referencing a load plane (grid surface) 
      //2.  not referencing a load plane, in which case a grid surface and axis needs to be created

      var gridSurfaceKeyword = GsaRecord.Keyword<GsaGridSurface>();
      var gridPlaneKeyword = GsaRecord.Keyword<GsaGridPlane>();

      StructuralAxis axis = null;
      int gridSurfaceIndex = 0;
      if (string.IsNullOrEmpty(loadPanel.LoadPlaneRef))
      {
        //Create axis based on the polyline
        try
        {
          axis = SpeckleStructuralGSA.Helper.Parse2DAxis(originalPolyline);
          var gsaAxisGwa = StructuralAxisToNative.ToNative(axis);
          gwaList.Add(gsaAxisGwa);

          // TO DO: review ways around having to parse here to get the newly - created axis index
          Initialiser.Interface.ParseGeneralGwa(gsaAxisGwa, out string _, out int? axisIndex, out string _, out string _, out string _, out GwaSetCommandType? _);

          var gridPlaneIndex = Initialiser.Cache.ResolveIndex(gridPlaneKeyword);
          var gsaGridPlane = new GsaGridPlane()
          {
            Index = gridPlaneIndex,
            Name = loadPanel.Name,
            AxisRefType = GridPlaneAxisRefType.Reference,
            AxisIndex = axisIndex,
            Elevation = AxisElevation(axis, originalPolyline),
            Type = GridPlaneType.General,
            StoreyToleranceAboveAuto = true,
            StoreyToleranceBelowAuto = true
          };
          if (gsaGridPlane.Gwa(out var gsaGridPlaneGwas, true))
          {
            gwaList.AddRange(gsaGridPlaneGwas);
          }

          gridSurfaceIndex = Initialiser.Cache.ResolveIndex(gridSurfaceKeyword);
          var gsaGridSurface = new GsaGridSurface()
          {
            Index = gridSurfaceIndex,
            PlaneIndex = gridPlaneIndex,
            Name = loadPanel.Name,
            AllIndices = true,
            Type = GridSurfaceElementsType.TwoD,
            Span = GridSurfaceSpan.One,
            Angle = 0,
            Tolerance = 0.01,
            Expansion = GridExpansion.Legacy
          };
          if (gsaGridSurface.Gwa(out var gsaGridSurfaceGwas, true))
          {
            gwaList.AddRange(gsaGridSurfaceGwas);
          }
        }
        catch
        {
          Initialiser.AppUI.Message("Generating axis from coordinates for 2D load panel", loadPanel.ApplicationId);
        }
      }
      else
      {
        //Get axis from load plane using LoadPlaneRef
        //Within this option, there are two routes to retrieve the axis:
        //1.  the StructuralLoadPlane has its own axis (because AxisRefs aren't offered yet in the Structural classes)
        //2.  the StructuralLoadPlane references a StructuralStorey, which has an axis

        gridSurfaceIndex = Initialiser.Cache.ResolveIndex(gridSurfaceKeyword, loadPanel.LoadPlaneRef);
        var loadPlanes = Initialiser.GSASenderObjects.Get<GSAGridSurface>();
        var matchingLoadPlanes = loadPlanes.Where(lp => lp.GSAId == gridSurfaceIndex);
        if (matchingLoadPlanes.Count() == 0)
        {
          Initialiser.AppUI.Message("Unable to resolve load plane reference", loadPanel.ApplicationId);
        }
        else
        {
          var loadPlane = ((StructuralLoadPlane)matchingLoadPlanes.First().Value);
          if (loadPlane.Axis == null)
          {
            var storeyIndex = Initialiser.Cache.ResolveIndex(gridPlaneKeyword, loadPlane.StoreyRef);

            var storeys = Initialiser.GSASenderObjects.Get<GSAStorey>();
            var matchingStoreys = storeys.Where(s => s.GSAId == storeyIndex);
            if (matchingStoreys.Count() == 0)
            {
              Initialiser.AppUI.Message("Unable to resolve load plane -> storey reference", loadPanel.ApplicationId);
            }
            else
            {
              var storey = ((StructuralStorey)matchingStoreys.First().Value);
              axis = storey.Axis;
            }
          }
          else
          {
            axis = loadPlane.Axis;
          }
        }
      }

      // Transform polygon coordinates to the relevant axis
      // keep in mind that the 2D load panel inherits from SpecklePolyline
      var polyline = SpeckleStructuralGSA.Helper.MapPointsGlobal2Local(originalPolyline, axis);

      foreach (var k in loadingDict.Keys)
      {
        var applicationId = string.Join("_", loadPanel.ApplicationId, k.ToString());
        var index = Initialiser.Cache.ResolveIndex(keyword, applicationId);

        var gsaLoadPanel = new GsaLoadGridArea()
        {
          Index = index,
          ApplicationId = applicationId,
          Name = loadPanel.Name,
          Value = loadingDict[k],
          GridSurfaceIndex = gridSurfaceIndex,
          LoadDirection = k,
          LoadCaseIndex = loadCaseIndex,
          //No support yet for an axis separate to the grid surface's, on which the loading is applied
          AxisRefType = AxisRefType.Global,
          //No support yet for whole-plane 2D load panels - all assumed to be based on polyline/polygon
          Area = LoadAreaOption.Polygon,
          Polygon = PolylineCoordsToGwaPolygon(polyline),
          Projected = false
        };
        if (gsaLoadPanel.Gwa(out var gsaLoadPanelGwas, true))
        {
          gwaList.AddRange(gsaLoadPanelGwas);
        }
      }

      return string.Join("\n", gwaList);
    }

    private static double AxisElevation(StructuralAxis axis, double[] polylineCoords)
    {
      // Calculate elevation
      var elevation = (polylineCoords[0] * axis.Normal.Value[0] +
          polylineCoords[1] * axis.Normal.Value[1] +
          polylineCoords[2] * axis.Normal.Value[2]) /
          Math.Sqrt(Math.Pow(axis.Normal.Value[0], 2) + Math.Pow(axis.Normal.Value[1], 2) + Math.Pow(axis.Normal.Value[2], 2));

      return elevation;
    }

    private static string PolylineCoordsToGwaPolygon(double[] coords)
    {
      var subLs = new List<string>();
      for (var j = 0; j < coords.Count(); j += 3)
      {
        //The GWA that GSA generates seems to return a rounded number, so do so here
        subLs.Add("(" + Math.Round(coords[j], 4).ToString() + "," + Math.Round(coords[j + 1], 4).ToString() + ")");
      }

      return "\"" + string.Join(" ", subLs) + "(m)\"";
    }

    private static Dictionary<LoadDirection3, double> ExplodeLoading(StructuralVectorThree loading)
    {
      var valueByDir = new Dictionary<LoadDirection3, double>();

      for (var i = 0; i < loadDirSeq.Count(); i++)
      {
        if (loading.Value[i] != 0)
        {
          valueByDir.Add(loadDirSeq[i], loading.Value[i]);
        }
      }

      return valueByDir;
    }
  }
}
