﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAConversion("LOAD_GRID_AREA.2", new string[] { "POLYLINE.1", "GRID_SURFACE.1", "GRID_PLANE.4", "AXIS" }, "elements", true, true, new Type[] { }, new Type[] { typeof(GSALoadCase) })]
  public class GSAGridAreaLoad : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DLoadPanel();

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      Structural2DLoadPanel obj = new Structural2DLoadPanel();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      var (gridPlaneRefRet, gridSurfaceRec) = Initialiser.Interface.GetGridPlaneRef(Convert.ToInt32(pieces[counter++]));
      var (gridPlaneAxis, gridPlaneElevation, gridPlaneRec) = Initialiser.Interface.GetGridPlaneData(gridPlaneRefRet);
      this.SubGWACommand.Add(gridSurfaceRec);
      this.SubGWACommand.Add(gridPlaneRec);

      string gwaRec = null;
      StructuralAxis axis = HelperClass.Parse0DAxis(gridPlaneAxis, Initialiser.Interface, out gwaRec);
      if (gwaRec != null)
        this.SubGWACommand.Add(gwaRec);
      double elevation = gridPlaneElevation;

      string polylineDescription = "";

      switch (pieces[counter++])
      {
        case "PLANE":
          // TODO: Do not handle for now
          return;
        case "POLYREF":
          string polylineRef = pieces[counter++];
          string newRec = null;
          (polylineDescription, newRec) = Initialiser.Interface.GetPolylineDesc(Convert.ToInt32(polylineRef));
          this.SubGWACommand.Add(newRec);
          break;
        case "POLYGON":
          polylineDescription = pieces[counter++];
          break;
      }
      double[] polyVals = HelperClass.ParsePolylineDesc(polylineDescription);

      for (int i = 2; i < polyVals.Length; i += 3)
        polyVals[i] = elevation;

      obj.Value = HelperClass.MapPointsLocal2Global(polyVals, axis).ToList();
      obj.Closed = true;

      obj.LoadCaseRef = Initialiser.Interface.GetSID(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      int loadAxisId = 0;
      string loadAxisData = pieces[counter++];
      StructuralAxis loadAxis;
      if (loadAxisData == "LOCAL")
        loadAxis = axis;
      else
      {
        loadAxisId = loadAxisData == "GLOBAL" ? 0 : Convert.ToInt32(loadAxisData);
        loadAxis = HelperClass.Parse0DAxis(loadAxisId, Initialiser.Interface, out gwaRec);
        if (gwaRec != null)
          this.SubGWACommand.Add(gwaRec);
      }
      bool projected = pieces[counter++] == "YES";
      string direction = pieces[counter++];
      double value = Convert.ToDouble(pieces[counter++]);

      obj.Loading = new StructuralVectorThree(new double[3]);
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = value;
          break;
        case "Y":
          obj.Loading.Value[1] = value;
          break;
        case "Z":
          obj.Loading.Value[2] = value;
          break;
        default:
          // TODO: Error case maybe?
          break;
      }
      obj.Loading.TransformOntoAxis(loadAxis);

      if (projected)
      {
        double scale = (obj.Loading.Value[0] * axis.Normal.Value[0] +
            obj.Loading.Value[1] * axis.Normal.Value[1] +
            obj.Loading.Value[2] * axis.Normal.Value[2]) /
            (axis.Normal.Value[0] * axis.Normal.Value[0] +
            axis.Normal.Value[1] * axis.Normal.Value[1] +
            axis.Normal.Value[2] * axis.Normal.Value[2]);

        obj.Loading = new StructuralVectorThree(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2]);
        obj.Loading.Scale(scale);
      }

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural2DLoadPanel load = this.Value as Structural2DLoadPanel;

      if (load.Loading == null)
        return;

      string keyword = typeof(GSAGridAreaLoad).GetGSAKeyword();

      int polylineIndex = GSA.Indexer.ResolveIndex("POLYLINE.1", load.ApplicationId);
      int gridSurfaceIndex = GSA.Indexer.ResolveIndex("GRID_SURFACE.1", load.ApplicationId);
      int gridPlaneIndex = GSA.Indexer.ResolveIndex("GRID_PLANE.4", load.ApplicationId);

      int loadCaseRef = 0;
      try
      {
        loadCaseRef = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword().GetGSAKeyword(), load.LoadCaseRef).Value;
      }
      catch { loadCaseRef = GSA.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef); }

      //StructuralAxis axis = HelperClass.Parse2DAxis(load.Value.ToArray());
      var axis = HelperClass.Parse2DAxis(load.Value.ToArray());

      // Calculate elevation
      double elevation = (load.Value[0] * axis.Normal.Value[0] +
          load.Value[1] * axis.Normal.Value[1] +
          load.Value[2] * axis.Normal.Value[2]) /
          Math.Sqrt(axis.Normal.Value[0] * axis.Normal.Value[0] +
              axis.Normal.Value[1] * axis.Normal.Value[1] +
              axis.Normal.Value[2] * axis.Normal.Value[2]);

      // Transform coordinate to new axis
      //double[] transformed = GSA.MapPointsGlobal2Local(load.Value.ToArray(), axis);
      var transformed = HelperClass.MapPointsGlobal2Local(load.Value.ToArray(), axis);

      List<string> ls = new List<string>();

      string[] direction = new string[3] { "X", "Y", "Z" };

      for (int i = 0; i < load.Loading.Value.Count(); i++)
      {
        if (load.Loading.Value[i] == 0) continue;

        ls.Clear();

        //int index = GSA.Indexer.ResolveIndex(typeof(GSAGridAreaLoad).GetGSAKeyword());
        var index = GSA.Indexer.ResolveIndex(typeof(GSAGridAreaLoad).GetGSAKeyword());

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        //ls.Add(keyword + ":" + HelperClass.GenerateSID(load));
        ls.Add(keyword + ":" + HelperClass.GenerateSID(load));
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
        ls.Add(gridSurfaceIndex.ToString());
        ls.Add("POLYGON");
        List<string> subLs = new List<string>();
        for (int j = 0; j < transformed.Count(); j += 3)
          subLs.Add("(" + transformed[j].ToString() + "," + transformed[j + 1].ToString() + ")");
        ls.Add(string.Join(" ", subLs));
        ls.Add(loadCaseRef.ToString());
        ls.Add("GLOBAL");
        ls.Add("NO");
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
      }

      ls.Clear();
      ls.Add("SET");
      //ls.Add("GRID_SURFACE.1" + ":" + HelperClass.GenerateSID(load));
      ls.Add("GRID_SURFACE.1" + ":" + HelperClass.GenerateSID(load));
      ls.Add(gridSurfaceIndex.ToString());
      ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
      ls.Add(gridPlaneIndex.ToString());
      ls.Add("2"); // Dimension of elements to target
      ls.Add("all"); // List of elements to target
      ls.Add("0.01"); // Tolerance
      ls.Add("ONE"); // Span option
      ls.Add("0"); // Span angle
      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));

      ls.Clear();
      ls.Add("SET");
      //ls.Add("GRID_PLANE.4" + ":" + HelperClass.GenerateSID(load));
      ls.Add("GRID_PLANE.4" + ":" + HelperClass.GenerateSID(load));
      ls.Add(gridPlaneIndex.ToString());
      ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
      ls.Add("GENERAL"); // Type
      //ls.Add(GSA.SetAxis(axis, load.Name).ToString());
      ls.Add(HelperClass.SetAxis(axis, load.Name).ToString());
      ls.Add(elevation.ToString());
      ls.Add("0"); // Elevation above
      ls.Add("0"); // Elevation below
      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural2DLoadPanel load)
    {
      //new GSAGridAreaLoad() { Value = load }.SetGWACommand(Initialiser.Interface);
      new GSAGridAreaLoad() { Value = load }.SetGWACommand(Initialiser.Interface);
      return true;
    }

    public static SpeckleObject ToSpeckle(this GSAGridAreaLoad dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSAGridAreaLoad)))
        Initialiser.GSASenderObjects[typeof(GSAGridAreaLoad)] = new List<object>();

      List<GSAGridAreaLoad> loads = new List<GSAGridAreaLoad>();

      string keyword = typeof(GSAGridAreaLoad).GetGSAKeyword();
      string[] subKeywords = typeof(GSAGridAreaLoad).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSAGridAreaLoad)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSAGridAreaLoad)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSAGridAreaLoad load = new GSAGridAreaLoad() { GWACommand = p };
        load.ParseGWACommand(Initialiser.Interface);
        loads.Add(load);
      }

      Initialiser.GSASenderObjects[typeof(GSAGridAreaLoad)].AddRange(loads);

      if (loads.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
