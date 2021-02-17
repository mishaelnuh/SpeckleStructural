﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.SECTION, GwaSetCommandType.Set, true, true, true, GwaKeyword.MAT_CONCRETE ,GwaKeyword.MAT_STEEL)]
  public class GsaSection : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public Section1dType Type;
    public int? PoolIndex;
    public ReferencePoint ReferencePoint;
    public double? RefY;
    public double? RefZ;
    public double? Mass;
    public double? Fraction;
    public double? Cost;
    public double? Left;
    public double? Right;
    public double? Slab;
    public List<GsaSectionComponentBase> Components;
    //This would be populated by the final value before environment variables, which is either ENVIRON or NO_ENVIRON
    //- this isn't implemented yet in the FromGwa case
    public bool Environ = false;

    //private List<Type> sectionCompTypes = new List<Type>() {  typeof(Section)}
    private static readonly List<Type> SectionCompTypes = Helper.GetEnumerableOfType<GsaSectionComponentBase>().ToList();

    public GsaSection() : base()
    {
      //Defaults
      Version = 7;
    }

    //Notes abou the documentation:
    //- it leaves out the last 3 parameters
    //- mistakenly leaves out the pipe between 'slab' and 'num'
    //- the 'num' doesn't seem to represent the number of components at all (e.g. it is 1 even with 3 components), just whether there's at least one
    //- there is no mention of the last 3 arguments 0 | 0 | NO_ENVIRON/ENVIRON

    //SECTION.7 | ref | colour | name | memb | pool | point | refY | refZ | mass | fraction | cost | left | right | slab | num { < comp > } | 0 | 0 | NO_ENVIRON
    // where <comp> could be one or more of:
    //SECTION_COMP | ref | name | matAnal | matType | matRef | desc | offset_y | offset_z | rotn | reflect | pool
    //SECTION_CONC | ref | grade | agg
    //SECTION_STEEL | ref | grade | plasElas | netGross | exposed | beta | type | plate | lock
    //SECTION_LINK (not in documentation)
    //SECTION_COVER | ref | type:UNIFORM | cover | outer 
    //    or SECTION_COVER | ref | type:VARIABLE | top | bot | left | right | outer
    //    or SECTION_COVER | ref | type:FACE | num | face[] | outer
    //SECTION_TMPL (not in documentation except for a mention that is deprecated, despite being included in GWA generated by GSA 10.1

    public override bool FromGwa(string gwa)
    {
      //Because this GWA is actually comprised of a SECTION proper plus embedded SECTION_COMP and other components
      if (!ProcessComponents(gwa, out var gwaSectionProper))
      {
        return false;
      }

      //Assume the first partition is the one for SECTION proper
      if (!BasicFromGwa(gwaSectionProper, out var items))
      {
        return false;
      }

      var numComponents = 0;
      //SECTION.7 | ref | colour | name | memb | pool | point | refY | refZ | mass | fraction | cost | left | right | slab | num { < comp > } | 0 | 0 | NO_ENVIRON
      if (!(FromGwaByFuncs(items, out var remainingItems, (v) => Enum.TryParse(v, true, out Colour), AddName, (v) => v.TryParseStringValue(out Type), 
          (v) => AddNullableIndex(v, out PoolIndex), (v) => v.TryParseStringValue(out ReferencePoint), 
          (v) => AddNullableDoubleValue(v, out RefY), (v) => AddNullableDoubleValue(v, out RefZ),
          (v) => AddNullableDoubleValue(v, out Mass), (v) => AddNullableDoubleValue(v, out Fraction), (v) => AddNullableDoubleValue(v, out Cost),
          (v) => AddNullableDoubleValue(v, out Left), (v) => AddNullableDoubleValue(v, out Right), (v) => AddNullableDoubleValue(v, out Slab),
          (v) => int.TryParse(v, out numComponents))))
      {
        return false;
      }

      //The final partition should have, tacked onto its end, the final three items of the entire original GWA (0 | 0 | NO_ENVIRON).
      //For now, leave these where they are as they should be ignored by the FromGwa of whatever is the final partition (section component) anyway

      //This check reflects the fact that the num parameter
      return ((numComponents == 0 && (Components == null || Components.Count() == 0)) || (numComponents == 1 && Components.Count() > 0));
    }

    //Doesn't take version into account yet
    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //SECTION.7 | ref | colour | name | memb | pool | point | refY | refZ | mass | fraction | cost | left | right | slab | num { < comp > } | 0 | 0 | NO_ENVIRON
      AddItems(ref items, Colour.ToString(), Name, Type.GetStringValue(), PoolIndex ?? 0, ReferencePoint.GetStringValue(), 
        RefY ?? 0, RefZ ?? 0, Mass ?? 0, Fraction ?? 0, Cost ?? 0, Left ?? 0, Right ?? 0, Slab ?? 0, 
        Components == null || Components.Count() == 0 ? 0 : 1);

      ProcessComponents(ref items);

      AddItems(ref items, 0, 0, Environ ? "ENVIRON" : "NO_ENVIRON");

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();

      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private bool ProcessComponents(ref List<string> items)
    {
      foreach (var comp in Components)
      {
        if (comp.GwaItems(out var compItems, false, false))
        {
          items.AddRange(compItems);
        }
      }
      return true;
    }
    #endregion

    #region from_gwa_fns

    private bool ProcessComponents(string gwa, out string gwaSectionProper)
    {
      //This will only catch the section component keywords that have been implemented.  This will mean the GWA of any other as-yet-not-implemented
      //section components will be the trailing end of either the SECTION proper or one of the implemented section components.
      //This will be picked up later - for now just return the partitions based on the implemented section types' keywords
      var sectionCompTypesByKeywords = SectionCompTypes.ToDictionary(t => (GwaKeyword)t.GetAttribute<GsaType>("Keyword"), t => t);

      //First break up the GWA into the SECTION proper and the components
      var sectionCompStartIndicesTypes = new Dictionary<int, Type>();
      foreach (var sckw in sectionCompTypesByKeywords.Keys)
      {
        var index = gwa.IndexOf(sckw.GetStringValue());
        if (index > 0)
        {
          sectionCompStartIndicesTypes.Add(index, sectionCompTypesByKeywords[sckw]);
        }
      }
      var orderedComponentStartIndices = sectionCompStartIndicesTypes.Keys.OrderBy(i => i).ToList();

      var gwaPieces = new List<string>();
      var startIndex = 0;
      foreach (var i in orderedComponentStartIndices)
      {
        var gwaPartition = gwa.Substring(startIndex, i - startIndex);
        gwaPieces.Add(gwaPartition.TrimEnd('\t'));
        startIndex = i;
      }
      gwaPieces.Add(gwa.Substring(startIndex));

      gwaSectionProper = gwaPieces.First();

      var sectionComps = new List<GsaSectionComponentBase>();
      var partitionIndex = 1;
      foreach (var i in orderedComponentStartIndices)
      {
        var sectionComp = (GsaSectionComponentBase)Activator.CreateInstance(sectionCompStartIndicesTypes[i]);
        sectionComp.FromGwa(gwaPieces[partitionIndex++]);
        startIndex = i;

        if (Components == null)
        {
          Components = new List<GsaSectionComponentBase>();
        }
        Components.Add(sectionComp);
      }

      
      return true;
    }
    #endregion
  }
}
