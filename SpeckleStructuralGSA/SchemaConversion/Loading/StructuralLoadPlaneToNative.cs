﻿using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleGSAInterfaces;
using System.Collections.Generic;

namespace SpeckleStructuralGSA.SchemaConversion
{
  //Load plane corresponds to GRID_SURFACE
  public static class StructuralLoadPlaneToNative
  {
    public static string ToNative(this StructuralLoadPlane loadPlane)
    {
      var gwaCommands = new List<string>();

      var keyword = GsaRecord.Keyword<GsaGridSurface>();

      var index = Initialiser.Cache.ResolveIndex(keyword, loadPlane.ApplicationId);
      var gsaGridSurface = new GsaGridSurface()
      {
        Index = index,
        Name = loadPlane.Name,
        Tolerance = loadPlane.Tolerance,
        Angle = loadPlane.SpanAngle,
        
        Type = (loadPlane.ElementDimension.HasValue && loadPlane.ElementDimension.Value == 1)
          ? GridSurfaceElementsType.OneD
          : (loadPlane.ElementDimension.HasValue && loadPlane.ElementDimension.Value == 2)
            ? GridSurfaceElementsType.TwoD
            : GridSurfaceElementsType.NotSet,

        Span = (loadPlane.Span.HasValue && loadPlane.Span.Value == 1)
          ? GridSurfaceSpan.One
          : (loadPlane.Span.HasValue && loadPlane.Span.Value == 2)
            ? GridSurfaceSpan.Two
            : GridSurfaceSpan.NotSet,

        //There is no support for entity references in the structural schema, so select "all" here
        AllIndices = true,

        //There is no support for this argument in the Structural schema, and was even omitted from the GWA 
        //in the previous version of the ToNative code
        Expansion = GridExpansion.Legacy
      };

      if (loadPlane.Axis.ValidNonZero())
      {
        gsaGridSurface.PlaneRefType = GridPlaneAxisRefType.Reference;

        //Create axis
        //Create new axis on the fly here
        var gsaAxisGwa = StructuralAxisToNative.ToNative(loadPlane.Axis);
        gwaCommands.Add(gsaAxisGwa);

        //TO DO: review ways around having to parse here to get the newly-created axis index
        Initialiser.Interface.ParseGeneralGwa(gsaAxisGwa, out var _, out var axisIndex, out var _, out var _, out var _, out var _);

        //Create plane - the key here is that it's not a storey, but a general, type of grid plane, 
        //which is why the ToNative() method for SpeckleStorey shouldn't be used as it only creates storey-type GSA grid plane
        var gsaPlaneKeyword = GsaRecord.Keyword<GsaGridPlane>();
        var planeIndex = Initialiser.Cache.ResolveIndex(gsaPlaneKeyword);
        
        var gsaPlane = new GsaGridPlane()
        {
          Index = planeIndex,
          Name = loadPlane.Name,
          Type = GridPlaneType.General,
          AxisRefType = GridPlaneAxisRefType.Reference,
          AxisIndex = axisIndex
        };
        if (gsaPlane.Gwa(out var gsaPlaneGwas, true))
        {
          gwaCommands.AddRange(gsaPlaneGwas);
        }
      }
      else
      {
        gsaGridSurface.PlaneRefType = GridPlaneAxisRefType.Global;
      }

      if (gsaGridSurface.Gwa(out var gwaLines, true))
      {
        gwaCommands.AddRange(gwaLines);
        return string.Join("\n", gwaCommands);
      }

      return "";
    }
  }
}
