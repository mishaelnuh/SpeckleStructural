﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("EL.4", new string[] { "NODE.3" }, "elements", true, false, new Type[] { typeof(GSANode), typeof(GSA1DProperty), typeof(GSASpringProperty) }, new Type[] { typeof(GSANode), typeof(GSA1DProperty), typeof(GSASpringProperty) })]
  public class GSA1DElement : IGSASpeckleContainer
  {
    public string Member;
    
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElement();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      // GWA command from 10.1 docs
      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle |
      // is_rls { | rls { | k } }
      // off_x1 | off_x2 | off_y | off_z | parent_member | dummy

      if (this.GWACommand == null)
        return;

      var obj = new Structural1DElement();

      var pieces = this.GWACommand.ListSplit("\t");
      
      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);

      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Colour
      counter++; // Type
      var propRef = pieces[counter++];
      //Sometimes the property ref argument seems to have a relative length span attached to it:
      //e.g. 1[0.245882:0.491765] where 1 is the actual property reference
      var propRefNumerical = "";
      var index = 0;
      while (index < propRef.Length && char.IsDigit(propRef[index]))
      {
        propRefNumerical += propRef[index++];
      }
      
      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA1DProperty).GetGSAKeyword(), Convert.ToInt32(propRefNumerical));
      counter++; // Group

      obj.Value = new List<double>();
      for (var i = 0; i < 2; i++)
      {
        var key = pieces[counter++];
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
        node.ForceSend = true;
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      var orientationNodeRef = pieces[counter++];
      var rotationAngle = Convert.ToDouble(pieces[counter++]);

      try
      {
        if (orientationNodeRef != "0")
        {
          var node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
          node.ForceSend = true;

          obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle, node.Value.Value.ToArray()).Normal as StructuralVectorThree;
          this.SubGWACommand.Add(node.GWACommand);
        }
        else
        {
          obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;
        }
      }
      catch
      {
        Initialiser.AppUI.Message("Generating axis from coordinates for 1D element", obj.ApplicationId);
      }

      if (pieces[counter++] != "NO_RLS")
      {
        var start = pieces[counter++];
        var end = pieces[counter++];

        var endReleases = new List<StructuralVectorBoolSix>
        {
          new StructuralVectorBoolSix(new bool[6]),
          new StructuralVectorBoolSix(new bool[6])
        };

        endReleases[0].Value[1] = ParseEndRelease(start[1], pieces, ref counter);
        endReleases[0].Value[2] = ParseEndRelease(start[2], pieces, ref counter);
        endReleases[0].Value[3] = ParseEndRelease(start[3], pieces, ref counter);
        endReleases[0].Value[4] = ParseEndRelease(start[4], pieces, ref counter);
        endReleases[0].Value[5] = ParseEndRelease(start[5], pieces, ref counter);

        endReleases[1].Value[0] = ParseEndRelease(end[0], pieces, ref counter);
        endReleases[1].Value[1] = ParseEndRelease(end[1], pieces, ref counter);
        endReleases[1].Value[2] = ParseEndRelease(end[2], pieces, ref counter);
        endReleases[1].Value[3] = ParseEndRelease(end[3], pieces, ref counter);
        endReleases[1].Value[4] = ParseEndRelease(end[4], pieces, ref counter);
        endReleases[1].Value[5] = ParseEndRelease(end[5], pieces, ref counter);

        obj.EndRelease = endReleases;
      }
      else
      {
        obj.EndRelease = new List<StructuralVectorBoolSix>
        {
          new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true }),
          new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true })
        };
      }

      var offsets = new List<StructuralVectorThree>
      {
        new StructuralVectorThree(new double[3]),
        new StructuralVectorThree(new double[3])
      };

      offsets[0].Value[0] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[0] = Convert.ToDouble(pieces[counter++]);

      offsets[0].Value[1] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[1] = offsets[0].Value[1];

      offsets[0].Value[2] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[2] = offsets[0].Value[2];

      obj.Offset = offsets;

      counter++; // Dummy

      if (counter < pieces.Length)
      {
        Member = pieces[counter++]; // no references to this piece of data, why do we store it rather than just skipping over?
        if (int.TryParse(Member, out var memberIndex))
        {
          obj.ApplicationId = Helper.GetApplicationId(typeof(GSA1DMember).GetGSAKeyword(), memberIndex);
        }
      }
      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var element = this.Value as Structural1DElement;

      if (element.Value == null || element.Value.Count() == 0)
        return "";

      var keyword = typeof(GSA1DElement).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(keyword, element.ApplicationId);

      var propKeyword = (element.ElementType == Structural1DElementType.Spring)
        ? typeof(GSASpringProperty).GetGSAKeyword()
        : typeof(GSA1DProperty).GetGSAKeyword();

      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, element.PropertyRef);

      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, element.PropertyRef);
      if (indexResult == null && element.ApplicationId != null)
      {
        if (element.PropertyRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", element.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", element.ApplicationId + " referencing " + element.PropertyRef);
        }
      }

      var sid = Helper.GenerateSID(element);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        element.Name == null || element.Name == "" ? " " : element.Name,
        "NO_RGB",
        "BEAM", // Type
        propRef.ToString(), // Prop
        group.ToString() // Group
      };
      
      // topo()
      for (var i = 0; i < element.Value.Count(); i += 3)
      {
        ls.Add(Helper.NodeAt(element.Value[i], element.Value[i + 1], element.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString());
      }
      
      ls.Add("0"); // Orientation Node
      
      // orient_angle
      try
      {
        ls.Add(Helper.Get1DAngle(element.Value.ToArray(), element.ZAxis).ToString());
      }
      catch { ls.Add("0"); }
      
      // is_rls { | k }
      try
      {
        var subLs = new List<string>();
        if (element.EndRelease[0].Value.Any(x => x) || element.EndRelease[1].Value.Any(x => x))
        {
          subLs.Add("RLS");

          var end1 = "";

          end1 += element.EndRelease[0].Value[0] ? "R" : "F";
          end1 += element.EndRelease[0].Value[1] ? "R" : "F";
          end1 += element.EndRelease[0].Value[2] ? "R" : "F";
          end1 += element.EndRelease[0].Value[3] ? "R" : "F";
          end1 += element.EndRelease[0].Value[4] ? "R" : "F";
          end1 += element.EndRelease[0].Value[5] ? "R" : "F";

          subLs.Add(end1);

          var end2 = "";

          end2 += element.EndRelease[1].Value[0] ? "R" : "F";
          end2 += element.EndRelease[1].Value[1] ? "R" : "F";
          end2 += element.EndRelease[1].Value[2] ? "R" : "F";
          end2 += element.EndRelease[1].Value[3] ? "R" : "F";
          end2 += element.EndRelease[1].Value[4] ? "R" : "F";
          end2 += element.EndRelease[1].Value[5] ? "R" : "F";

          subLs.Add(end2);

          ls.AddRange(subLs);
        }
        else
          ls.Add("NO_RLS");
      }
      catch { ls.Add("NO_RLS"); }

      // off_x1 | off_x2 | off_y | off_z
      try
      {
        var subLs = new List<string>
        {
          element.Offset[0].Value[0].ToString(), // Offset x-start
          element.Offset[1].Value[0].ToString(), // Offset x-end

          element.Offset[0].Value[1].ToString(),
          element.Offset[0].Value[2].ToString()
        };

        ls.AddRange(subLs);
      }
      catch
      {
        ls.Add("0");
        ls.Add("0");
        ls.Add("0");
        ls.Add("0");
      }

      ls.Add(""); // parent_member

      ls.Add((element.GSADummy.HasValue && element.GSADummy.Value) ? "DUMMY" : ""); // dummy

      return (string.Join("\t", ls));
    }

    private static bool ParseEndRelease(char code, string[] pieces, ref int counter)
    {
      switch (code)
      {
        case 'F':
          return false;
        case 'R':
          return true;
        default:
          // TODO
          counter++;
          return true;
      }
    }
  }

  [GSAObject("MEMB.8", new string[] { "NODE.3" }, "elements", false, true, new Type[] { typeof(GSA1DProperty), typeof(GSANode), typeof(GSASpringProperty) }, new Type[] { typeof(GSA1DProperty), typeof(GSANode), typeof(GSASpringProperty) })]
  public class GSA1DMember : IGSASpeckleContainer
  {
    public int Group; // Keep for load targetting

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElement();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      // MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 }
      // rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      // 
      // MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 }
      // rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      // 
      // MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 }
      // rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | }
      // load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }

      if (this.GWACommand == null)
        return;

      var obj = new Structural1DElement();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // num - Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' }); // name
      counter++; // colour

      // type(1D)
      var type = pieces[counter++];
      if (type == "BEAM")
        obj.ElementType = Structural1DElementType.Beam;
      else if (type == "COLUMN")
        obj.ElementType = Structural1DElementType.Column;
      else if (type == "CANTILEVER")
        obj.ElementType = Structural1DElementType.Cantilever; // doesnt appear to be an option in GSA10.1
      else
        obj.ElementType = Structural1DElementType.Generic;

      counter++; // exposure - fire property e.g. TOP_BOT - not currently supported
      var propId = Convert.ToInt32(pieces[counter++]);
      
      this.Group = Convert.ToInt32(pieces[counter++]); // group - Keep group for load targetting

      // topology
      obj.Value = new List<double>();
      var nodeRefs = pieces[counter++].ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(nodeRefs[i])).FirstOrDefault();
        if (node == null)
        {
          //TO DO: review how this is possible and prevent it
          continue;
        }
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      // orientation
      var orientationNodeRef = pieces[counter++]; // node - aka orientation node
      var rotationAngle = Convert.ToDouble(pieces[counter++]); // angle

      if (orientationNodeRef != "0")
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(),
            rotationAngle, node.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;

      var meshSize = Convert.ToDouble(pieces[counter++]);
      // since this is a nullable GSA-specific property(and therefore needs a review), only set if not default
      if (meshSize > 0)
      {
        obj.GSAMeshSize = Convert.ToDouble(pieces[counter++]);
      }

      counter++; // is_intersector
      var analysisType = pieces[counter++]; // analysis_type
      if (analysisType == "SPRING")
      {
        obj.ElementType = Structural1DElementType.Spring;
      }

      obj.PropertyRef = Helper.GetApplicationId((obj.ElementType == Structural1DElementType.Spring ) 
        ? typeof(GSASpringProperty).GetGSAKeyword()
        : typeof(GSA1DProperty).GetGSAKeyword(), propId); // prop


      counter++; // fire
      counter++; // limiting temperature
      counter++; // time[] 1
      counter++; // time[] 2
      counter++; // time[] 3
      counter++; // time[] 4

      // dummy - since this is a nullable GSA-specific property (and therefore needs a review), only set if true
      if (pieces[counter++].ToLower() == "dummy")
      {
        obj.GSADummy = true;
      }

      // end releases
      var releases = new List<StructuralVectorBoolSix>();
      var endReleases = new List<StructuralVectorBoolSix>();
      if (counter < pieces.Length)
      {
        var end1Release = pieces[counter++].ToLower();
        endReleases.Add(ParseEndRelease(end1Release));
        if (end1Release.Contains('k'))
          counter++; // skip past spring stiffnesses
      }
      if (counter < pieces.Length)
      {
        var end2Release = pieces[counter++].ToLower();
        endReleases.Add(ParseEndRelease(end2Release));
        if (end2Release.Contains('k'))
          counter++; // skip past spring stiffnesses
      }

      if (endReleases.Count() > 0)
      {
        obj.EndRelease = endReleases;
      }

      // skip to offsets
      if(!pieces.Last().ToLower().StartsWith("no"))
      {
        // this approach ignores the auto / manual distinction in GSA
        // which may affect the true offset
        
        counter = pieces.Length - 4;

        var offsets = new List<StructuralVectorThree>
        {
          new StructuralVectorThree(new double[3]),
          new StructuralVectorThree(new double[3])
        };

        offsets[0].Value[0] = Convert.ToDouble(pieces[counter++]); // x1
        offsets[1].Value[0] = Convert.ToDouble(pieces[counter++]); // x2

        offsets[0].Value[1] = Convert.ToDouble(pieces[counter++]); // y
        offsets[1].Value[1] = offsets[0].Value[1]; // y

        offsets[0].Value[2] = Convert.ToDouble(pieces[counter++]); // z
        offsets[1].Value[2] = offsets[0].Value[2]; // z

        obj.Offset = offsets;
      }

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var member = this.Value as Structural1DElement;
      if (member.Value == null || member.Value.Count() == 0)
        return "";

      var keyword = typeof(GSA1DMember).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSA1DMember).GetGSAKeyword(), member.ApplicationId);

      var propKeyword = ((member.ElementType == Structural1DElementType.Spring) ? typeof(GSASpringProperty) : typeof(GSA1DProperty)).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, member.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, member.PropertyRef);
      if (indexResult == null && member.ApplicationId != null)
      {
        if (member.PropertyRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", member.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", member.ApplicationId + " referencing " + member.PropertyRef);
        }
      }

      var sid = Helper.GenerateSID(member);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        member.Name == null || member.Name == "" ? " " : member.Name,
        "NO_RGB"
      };
      if (member.ElementType == Structural1DElementType.Beam)
        ls.Add("BEAM");
      else if (member.ElementType == Structural1DElementType.Column)
        ls.Add("COLUMN");
      else if (member.ElementType == Structural1DElementType.Cantilever)
        ls.Add("CANTILEVER");
      else
        ls.Add("1D_GENERIC");
      ls.Add("ALL"); // fire exposure reference, default to worst case (also GSA default)
      ls.Add(propRef.ToString());
      ls.Add(group != 0 ? group.ToString() : index.ToString()); // TODO: This allows for targeting of elements from members group
      var topo = "";
      if (member.Value != null)
      {
        for (var i = 0; i < member.Value.Count(); i += 3)
        {
          topo += Helper.NodeAt(member.Value[i], member.Value[i + 1], member.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString() + " ";
        }
      }
      ls.Add(topo.TrimEnd());
      ls.Add("0"); // Orientation node
      if (member.Value == null)
      {
        ls.Add("0");
      }
      else
      {
        try
        {
          ls.Add(Helper.Get1DAngle(member.Value.ToArray(), member.ZAxis ?? new StructuralVectorThree(0, 0, 1)).ToString());
        }
        catch { ls.Add("0"); }
      }
      ls.Add(member.GSAMeshSize == null ? "0" : member.GSAMeshSize.ToString()); // Target mesh size
      ls.Add("YES"); // intersector - GSA default
      ls.Add((member.ElementType == Structural1DElementType.Spring) ? "SPRING" : "BEAM"); // analysis type - there are more options than this in GSA docs
      ls.Add("0"); // Fire
      ls.Add("0"); // Limiting temperature
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // Time 4
      ls.Add((member.GSADummy.HasValue && member.GSADummy.Value) ? "DUMMY" : "ACTIVE");

      if (member.EndRelease == null || member.EndRelease.Count != 2)
        ls.AddRange(new[] { EndReleaseToGWA(null), EndReleaseToGWA(null) });
      else
        ls.AddRange(new[] { EndReleaseToGWA(member.EndRelease[0]), EndReleaseToGWA(member.EndRelease[1]) });

      ls.Add("Free"); // restraint_end_1
      ls.Add("Free"); // restraint_end_2

      ls.Add("0"); // Effective length option
      ls.Add("0"); // height
      ls.Add("0"); // load_ref

      if (member.Offset == null)
      {
        ls.Add("NO_OFF");
      }
      else
      {
        ls.Add("YES");
        ls.Add("MAN");
        ls.Add("MAN");
        try
        {
          var subLs = new List<string>
        {
          member.Offset[0].Value[0].ToString(), // Offset x-start
          member.Offset[1].Value[0].ToString(), // Offset x-end

          member.Offset[0].Value[1].ToString(),
          member.Offset[0].Value[2].ToString()
        };

          ls.AddRange(subLs);
        }
        catch
        {
          ls.AddRange(new[] { "0", "0", "0", "0" });
        }
      }

      return (string.Join("\t", ls));
    }

    private static StructuralVectorBoolSix ParseEndRelease(string code)
    {
      if (code.Length != 6)
        throw new ArgumentException($"End release code must be exactly six characters long - input code '{code}'");

      bool[] releases = new bool[6];
      for(int i = 0; i < code.Length; i++)
      {
        char piece = code.ToLower()[i];
        if (piece == 'f')
          releases[i] = false;
        else
          releases[i] = true;
      }

      return new StructuralVectorBoolSix(releases);
    }

    private static string EndReleaseToGWA(StructuralVectorBoolSix release)
    {
      string code = "";

      if (release == null)
        return "FFFFFF"; // GSA default

      foreach (bool b in release.Value)
        if (b)
          code += "R";
        else
          code += "F";

      return code;
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpeckleLine inputObject)
    {
      var convertedObject = new Structural1DElement();

      foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        var inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static string ToNative(this Structural1DElement beam)
    {
      return (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) 
        ? new GSA1DElement() { Value = beam }.SetGWACommand()
        : new GSA1DMember() { Value = beam }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA1DElement dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DElement>();
      var typeName = dummyObject.GetType().Name;
      var elementsLock = new object();
      var elements = new List<GSA1DElement>();
      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

#if DEBUG
      foreach (var p in newLines.Values)
#else
      Parallel.ForEach(newLines.Values, p =>
#endif
      {
        var pPieces = p.ListSplit("\t");

        if (pPieces[4] == "BEAM" && pPieces[4].ParseElementNumNodes() == 2)
        {
          var gsaId = pPieces[1];
          try
          {
            var element = new GSA1DElement() { GWACommand = p };
            element.ParseGWACommand(nodes);
            lock (elementsLock)
            {
              elements.Add(element);
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppUI.Message(typeName + ": " + ex.Message, gsaId);
          }
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GSASenderObjects.AddRange(elements);

      return (elements.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA1DMember dummyObject)
    {
      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();
      var membersLock = new object();
      var members = new List<GSA1DMember>();
      var newLines = ToSpeckleBase<GSA1DMember>();
      var typeName = dummyObject.GetType().Name;

#if DEBUG
      foreach (var p in newLines.Values)
#else
      Parallel.ForEach(newLines.Values, p =>
#endif
      {
        var pPieces = p.ListSplit("\t");
        var gsaId = pPieces[1];
        if (pPieces[4].Is1DMember())
        {
          try
          {
            var member = new GSA1DMember() { GWACommand = p };
            member.ParseGWACommand(nodes);
            lock (membersLock)
            {
              members.Add(member);
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppUI.Message(typeName + ": " + ex.Message, gsaId);
          }
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GSASenderObjects.AddRange(members);

      return (members.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
