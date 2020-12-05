﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.MEMB, GwaSetCommandType.Set, StreamBucket.Model, true, false)]
  public class GsaMemb : GsaRecord
  {
    //Not supporting: 3D members, or 2D reinforcement
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public MemberType Type;

    #region members_for_1D_2D

    public ExposedSurfaces Exposure;
    public int? PropertyIndex;
    public int? Group;
    public List<int> NodeIndices;           //Perimeter/edge topology
    public List<List<int>> Voids;           //Void topologies corresponding to the V sections in the GWA string, like in "41 42 43 44 V(45 46 47 48)"
    public List<int> PointNodeIndices;      //Points to include, corresponding to the P list in the GWA string, like in "41 42 43 44 P(50 55)"
    public List<List<int>> Polylines;       //Polyline topologies correspoding to the L sections in the GWA string, like in "41 42 43 44 L(71 72 73)"
    public List<List<int>> AdditionalAreas; //Additional solid area topologies corresponding to the A sections in the GWA string, like in "41 42 43 44 A(45 46 47 48)"
    public int? OrientationNodeIndex;
    public double? Angle;
    public double? MeshSize;
    public bool IsIntersector;
    public AnalysisType AnalysisType;
    public FireResistance Fire;
    public double? LimitingTemperature;
    public int CreationFromStartDays;
    public int StartOfDryingDays;
    public int AgeAtLoadingDays;
    public int RemovedAtDays;
    public bool Dummy;

    #region members_1D
    public Dictionary<AxisDirection6, ReleaseCode> Releases1;
    public List<double> Stiffnesses1;
    public Dictionary<AxisDirection6, ReleaseCode> Releases2;
    public List<double> Stiffnesses2;
    public Restraint RestraintEnd1;
    public Restraint RestraintEnd2;
    public EffectiveLengthType EffectiveLengthType;
    public double? LoadHeight;
    public LoadHeightReferencePoint LoadHeightReferencePoint;
    public bool MemberHasOffsets;
    public bool End1AutomaticOffset;
    public bool End2AutomaticOffset;
    public double? End1OffsetX;
    public double? End2OffsetX;
    public double? OffsetY;
    public double? OffsetZ;

    #region members_1D_eff_len
    //Only one of each set of EffectiveLength__ and Fraction__ values will be filled.  This could be reviewed and refactored accordingly
    public double? EffectiveLengthYY;
    public double? PercentageYY;  //Range: 0-100%
    public double? EffectiveLengthZZ;
    public double? PercentageZZ;  //Range: 0-100%
    public double? EffectiveLengthLateralTorsional;
    public double? FractionLateralTorsional;  //Range: 0-100%
    #endregion

    #region members_1D_explicit
    //Supporting "shortcuts" only, not restraint definitions down to the level of rotational (F1, F2, XX, YY, ZZ) and translational (F1, F2, Z, Y) restraints
    public List<RestraintDefinition> SpanRestraints;
    public List<RestraintDefinition> PointRestraints;
    #endregion

    #endregion

    #region members_2D
    public double? Offset2dZ;
    public double? OffsetAutomaticInternal;
    //Stored as string now but some separation into separate members
    public string Reinforcement;
    #endregion

    #endregion

    public GsaMemb() : base()
    {
      Version = 8;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //First all the common items for 1D members

      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | } load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      if (!FromGwaByFuncs(items, out remainingItems, AddName, AddColour, (v) => v.TryParseStringValue(out Type), (v) => Enum.TryParse(v, true, out Exposure),
        (v) => AddNullableIndex(v, out PropertyIndex), (v) => AddNullableIntValue(v, out Group), AddTopology, (v) => AddNullableIndex(v, out OrientationNodeIndex),
        (v) => AddNullableDoubleValue(v, out Angle), (v) => AddNullableDoubleValue(v, out MeshSize), (v) => AddYesNoBoolean(v, out IsIntersector),
        (v) => Enum.TryParse(v, true, out AnalysisType), AddFire,
        (v) => AddNullableDoubleValue(v, out LimitingTemperature), (v) => int.TryParse(v, out CreationFromStartDays), (v) => int.TryParse(v, out StartOfDryingDays),
        (v) => int.TryParse(v, out AgeAtLoadingDays), (v) => int.TryParse(v, out RemovedAtDays), AddDummy))
      {
        return false;
      }
      items = remainingItems;

      //This assumes that rls_1 { | k_1 } rls_2 { | k_2 } is at the start of the items list
      if (!ProcessReleases(items, out remainingItems))
      {
        return false;
      }
      items = remainingItems;

      if (!FromGwaByFuncs(items, out remainingItems, (v) => v.TryParseStringValue(out RestraintEnd1), (v) => v.TryParseStringValue(out RestraintEnd2),
          (v) => v.TryParseStringValue(out EffectiveLengthType)))
      {
        return false;
      }
      items = remainingItems;

      if (EffectiveLengthType == EffectiveLengthType.EffectiveLength)
      {
        AddEffectiveLength(items[0], ref EffectiveLengthYY, ref PercentageYY);
        AddEffectiveLength(items[1], ref EffectiveLengthZZ, ref PercentageZZ);
        AddEffectiveLength(items[2], ref EffectiveLengthLateralTorsional, ref FractionLateralTorsional);
        items = items.Skip(3).ToList();
      }
      else if (EffectiveLengthType == EffectiveLengthType.Explicit)
      {
        if (!ProcessExplicit(items, out remainingItems))
        {
          return false;
        }
        items = remainingItems;
      }

      if (!FromGwaByFuncs(items, out remainingItems, (v) => AddNullableDoubleValue(v, out LoadHeight), (v) => v.TryParseStringValue(out LoadHeightReferencePoint), AddIsOffset))
      {
        return false;
      }
      items = remainingItems;

      return MemberHasOffsets ? ProcessOffsets(items) : true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = new List<string>();
      //Just supporting non-void 1D types at this stage
      if (!(Type == MemberType.Beam || Type == MemberType.Generic1d || Type == MemberType.Column) || !InitialiseGwa(includeSet, out var items))
      {
        return false;
      }

      var axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();

      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | } load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      AddItems(ref items, Name, Colour.NO_RGB.ToString(), Type.GetStringValue(), Exposure.ToString(), PropertyIndex ?? 0, Group ?? 0,
        AddTopology(), OrientationNodeIndex ?? 0, Angle, MeshSize ?? 0, IsIntersector ? "YES" : "NO", AddAnalysisType(), (int)Fire, LimitingTemperature ?? 0,
        CreationFromStartDays, StartOfDryingDays, AgeAtLoadingDays, RemovedAtDays, Dummy ? "DUMMY" : "ACTIVE");

      AddEndReleaseItems(ref items, Releases1, Stiffnesses1, axisDirs);
      AddEndReleaseItems(ref items, Releases2, Stiffnesses2, axisDirs);

      AddItems(ref items, RestraintEnd1.GetStringValue(), RestraintEnd2.GetStringValue(), EffectiveLengthType.GetStringValue());

      if (EffectiveLengthType == EffectiveLengthType.EffectiveLength)
      {
        AddItems(ref items,
          AddEffectiveLength(EffectiveLengthYY, PercentageYY),
          AddEffectiveLength(EffectiveLengthZZ, PercentageZZ),
          AddEffectiveLength(EffectiveLengthLateralTorsional, FractionLateralTorsional));
      }
      else if (EffectiveLengthType == EffectiveLengthType.Explicit)
      {
        AddExplicitItems(ref items, PointRestraints);
        AddExplicitItems(ref items, SpanRestraints);
      }

      AddItems(ref items, LoadHeight ?? 0, LoadHeightReferencePoint.GetStringValue(), MemberHasOffsets ? "OFF" : "NO_OFF");

      if (MemberHasOffsets)
      {
        AddItems(ref items, AddAutoOrMan(End1AutomaticOffset), AddAutoOrMan(End2AutomaticOffset), End1OffsetX ?? 0, End2OffsetX ?? 0, OffsetY ?? 0, OffsetZ ?? 0);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    private string AddTopology()
    {
      var topoPortions = new List<string>
      {
        string.Join(" ", NodeIndices)
      };

      if (Voids != null && Voids.Count() > 0 && Voids.Any(v => v != null && v.Count() > 0))
      {
        var topoVoids = new List<string>();
        foreach (var vList in Voids.Where(v => v != null))
        {
          topoVoids.Add("V(" + string.Join(" ", vList) + ")");
        }
        topoPortions.Add(string.Join(" ", topoVoids));
      }

      if (PointNodeIndices != null && PointNodeIndices.Count() > 0)
      {
        topoPortions.Add("P(" + string.Join(" ", PointNodeIndices) + ")");
      }

      if (Polylines != null && Polylines.Count() > 0)
      {
        topoPortions.Add("L(" + string.Join(" ", PointNodeIndices) + ")");
      }

      if (AdditionalAreas != null && AdditionalAreas.Count() > 0 && AdditionalAreas.Any(v => v != null && v.Count() > 0))
      {
        var topoAdditional = new List<string>();
        foreach (var vList in AdditionalAreas.Where(v => v != null))
        {
          topoAdditional.Add("V(" + string.Join(" ", vList) + ")");
        }
        topoPortions.Add(string.Join(" ", topoAdditional));
      }
      return string.Join(" ", topoPortions);
    }

    private string AddTime()
    {
      return string.Join(" ", new[] { CreationFromStartDays, StartOfDryingDays, AgeAtLoadingDays, RemovedAtDays });
    }

    private string AddAnalysisType()
    {
      //TO DO: some validation here to ensure a valid combination of MemberType and AnalysisType
      return AnalysisType.ToString();
    }

    private string AddAutoOrMan(bool val)
    {
      return (val ? "AUTO" : "MAN");
    }

    private string AddEffectiveLength(double? el, double? fraction)
    {
      return ((el == null || el.Value == 0) && fraction.HasValue && fraction.Value > 0)
        ? (fraction.Value + "%")
        : (el.HasValue)
            ? el.Value.ToString()
            : 0.ToString();
    }

    #region other_to_gwa_add_x_Items_fns
    private void AddEndReleaseItems(ref List<string> items, Dictionary<AxisDirection6, ReleaseCode> releases, List<double> stiffnesses, List<AxisDirection6> axisDirs)
    {
      var rls = "";
      var stiffnessIndex = 0;
      foreach (var d in axisDirs)
      {
        var releaseCode = (releases != null && releases.Count() > 0 && releases.ContainsKey(d)) ? releases[d] : ReleaseCode.Fixed;
        rls += releaseCode.GetStringValue();
        if (releaseCode == ReleaseCode.Stiff && releases.ContainsKey(d) && (++stiffnessIndex) < stiffnesses.Count())
        {
          stiffnesses.Add(stiffnesses[stiffnessIndex]);
        }
      }
      items.Add(rls);
      if (stiffnesses != null && stiffnesses.Count() > 0)
      {
        items.AddRange(stiffnesses.Select(s => s.ToString()));
      }
      return;
    }

    private void AddExplicitItems(ref List<string> items, List<RestraintDefinition> restraintDefinitions)
    {
      items.Add(restraintDefinitions.Count().ToString());
      //Let 0 mean "all" too in light of the fact that all is written as 0 in the GWA
      var allDef = restraintDefinitions.Where(rd => rd.All || rd.Index == 0);

      if (allDef.Count() > 0)
      {
        items.AddRange(new[] { 0.ToString(), allDef.First().Restraint.GetStringValue() });
        return;
      }
      var orderedRestrDef = restraintDefinitions.OrderBy(rd => rd.Index).ToList();
      foreach (var rd in orderedRestrDef)
      {
        items.AddRange(new[] { rd.Index.ToString(), rd.Restraint.GetStringValue() });
      }
      return;
    }
    #endregion
    #endregion

    #region from_gwa_fns

    private bool AddColour(string v)
    {
      if (!Enum.TryParse(v, true, out Colour))
      {
        Colour = Colour.NO_RGB;
      }
      return true;
    }

    private bool AddTopology(string v)
    {
      var bracketPieces = v.Split(new[] { '(', ')' }).Select(s => s.Trim()).ToList();
      if (bracketPieces.Count() > 1)
      {
        var listTypes = bracketPieces.Take(bracketPieces.Count() - 1).Select(bp => bp.Last()).ToList();
        for (var i = 0; i < listTypes.Count(); i++)
        {
          switch (char.ToUpper(listTypes[i]))
          {
            case 'V':
              if (Voids == null)
              {
                Voids = new List<List<int>>();
              }
              Voids.Add(StringToIntList(bracketPieces[i + 1]));
              break;
            case 'P':
              PointNodeIndices = StringToIntList(bracketPieces[i + 1]);
              break;
            case 'L':
              if (Polylines == null)
              {
                Polylines = new List<List<int>>();
              }
              Polylines.Add(StringToIntList(bracketPieces[i + 1]));
              break;
            case 'A':
              if (AdditionalAreas == null)
              {
                AdditionalAreas = new List<List<int>>();
              }
              AdditionalAreas.Add(StringToIntList(bracketPieces[i + 1]));
              break;
          }
        }
      }
      NodeIndices = StringToIntList(bracketPieces[0]);
      return true;
    }

    private bool AddFire(string v)
    {
      if (int.TryParse(v, out int fireMinutes))
      {
        Fire = (FireResistance)fireMinutes;
        return true;
      }
      return false;
    }

    private bool AddDummy(string v)
    {
      Dummy = v.Equals("DUMMY", StringComparison.InvariantCultureIgnoreCase);
      return true;
    }

    private bool ProcessReleases(List<string> items, out List<string> remainingItems)
    {
      remainingItems = items; //default in case of early exit of this method
      var axisDirs = Enum.GetValues(typeof(AxisDirection6)).OfType<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();

      var endReleases = new Dictionary<AxisDirection6, ReleaseCode>[2] { null, null };
      var endStiffnesses = new List<double>[2];

      var itemIndex = 0;
      for (var i = 0; i < 2; i++)
      {
        endReleases[i] = new Dictionary<AxisDirection6, ReleaseCode>();
        endStiffnesses[i] = new List<double>();

        var relCodes = items[itemIndex++];
        if (relCodes.Length < axisDirs.Count())
        {
          return false;
        }

        var numExpectedStiffnesses = 0;
        for (var j = 0; j < axisDirs.Count(); j++)
        {
          var upperCharCode = char.ToUpper(relCodes[j]);
          if (upperCharCode == 'K')
          {
            numExpectedStiffnesses++;
            endReleases[i].Add(axisDirs[j], ReleaseCode.Stiff);
          }
          else if (upperCharCode == 'R')
          {
            endReleases[i].Add(axisDirs[j], ReleaseCode.Released);
          }
          else
          {
            //For now, Fixed values aren't added as it's considered the default
            //endReleases[i].Add(axisDirs[j], ReleaseCode.Fixed);
          }
        }

        if (numExpectedStiffnesses > 0)
        {
          for (var k = 0; k < numExpectedStiffnesses; k++)
          {
            if (!double.TryParse(items[itemIndex++], out double stiffness))
            {
              return false;
            }
            endStiffnesses[i].Add(stiffness);
          }
        }
      }

      Releases1 = endReleases[0].Count() > 0 ? endReleases[0] : null;
      Releases2 = endReleases[1].Count() > 0 ? endReleases[1] : null;
      Stiffnesses1 = endStiffnesses[0].Count() > 0 ? endStiffnesses[0] : null;
      Stiffnesses2 = endStiffnesses[1].Count() > 0 ? endStiffnesses[1] : null;

      remainingItems = items.Skip(itemIndex).ToList();

      return true;
    }

    private bool AddEffectiveLength(string v, ref double? el, ref double? perc)
    {
      var val = v.Trim();
      double doubleVal;
      if (val.Contains("%"))
      {
        if (double.TryParse(val.Substring(0, val.Length - 1), out doubleVal))
        {
          perc = doubleVal;
          return true;
        }
        else
        {
          return false;
        }
      }
      else if (double.TryParse(val, out doubleVal))
      {
        el = doubleVal;
        return true;
      }
      else
      {
        return false;
      }
    }

    private bool ProcessExplicit(List<string> items, out List<string> remainingItems)
    {
      remainingItems = items; //default in case of early exit of this method
      //Assume the first item in the items list is the num points value in the GWA format for MEMB
      var itemIndex = 0;
      if (!int.TryParse(items[itemIndex++], out var numPts))
      {
        return false;
      }
      for (var i = 0; i < numPts; i++)
      {
        if (!int.TryParse(items[itemIndex++], out var ptIndex) || !items[itemIndex++].TryParseStringValue(out Restraint restraint))
        {
          return false;
        }
        if (PointRestraints == null)
        {
          PointRestraints = new List<RestraintDefinition>();
        }
        PointRestraints.Add(new RestraintDefinition() { All = (ptIndex == 0), Index = (ptIndex == 0) ? null : (int?)ptIndex, Restraint = restraint });
      }

      if (!int.TryParse(items[itemIndex++], out var numSpans))
      {
        return false;
      }
      for (var i = 0; i < numSpans; i++)
      {
        if (!int.TryParse(items[itemIndex++], out var spanIndex) || !items[itemIndex++].TryParseStringValue(out Restraint restraint))
        {
          return false;
        }
        if (SpanRestraints == null)
        {
          SpanRestraints = new List<RestraintDefinition>();
        }
        SpanRestraints.Add(new RestraintDefinition() { All = (spanIndex == 0), Index = (spanIndex == 0) ? null : (int?)spanIndex, Restraint = restraint });
      }
      remainingItems = items.Skip(itemIndex).ToList();
      return true;
    }

    private bool AddIsOffset(string v)
    {
      MemberHasOffsets = (v.Equals("off", StringComparison.InvariantCultureIgnoreCase));
      return true;
    }

    private bool ProcessOffsets(List<string> items)
    {
      if (items.Count() < 6)
      {
        return false;
      }
      End1AutomaticOffset = items[0].Equals("AUTO", StringComparison.InvariantCultureIgnoreCase);
      End2AutomaticOffset = items[1].Equals("AUTO", StringComparison.InvariantCultureIgnoreCase);
      return (AddNullableDoubleValue(items[2], out End1OffsetX) && AddNullableDoubleValue(items[3], out End2OffsetX)
        && AddNullableDoubleValue(items[4], out OffsetY) && AddNullableDoubleValue(items[5], out OffsetZ));
    }

    #endregion
  }

  public struct RestraintDefinition
  {
    public bool All;
    public int? Index;
    public Restraint Restraint;
  }

}
