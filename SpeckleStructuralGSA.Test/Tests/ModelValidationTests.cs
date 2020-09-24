﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using System.IO;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ModelValidationTests : TestBase
  {
    public ModelValidationTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [SetUp]
    public void BeforeEachTest()
    {
      Initialiser.Settings = new Settings();
    }

    internal class UnmatchedData
    {
      public List<string> Retrieved;
      public List<string> FromFile;
    }

    [Test]
    public void ReceiverGsaValidationSimple()
    {
      ReceiverGsaValidation("Simple", simpleDataJsonFileNames, GSATargetLayer.Design);
    }

    [Test]
    public void ReceiverGsaValidationNb()
    {
      ReceiverGsaValidation("NB", savedJsonFileNames, GSATargetLayer.Design);
    }

    [Test]
    public void ReceiverGsaValidationDd()
    {
      ReceiverGsaValidation("DD", ddDataJsonFileNames, GSATargetLayer.Design);
    }

    private void ReceiverGsaValidation(string subdir, string[] jsonFiles, GSATargetLayer layer)
    {
      // Takes a saved Speckle stream with structural objects
      // converts to GWA and sends to GSA
      // then reads the data back out of GSA
      // and compares the two sets of GWA
      // if successful then there will be the same number
      // of each of the keywords in as out

      SpeckleInitializer.Initialize();
      gsaInterfacer = new GSAProxy();
      gsaCache = new GSACache();

      Initialiser.Cache = gsaCache;
      Initialiser.Interface = gsaInterfacer;
      Initialiser.AppUI = new SpeckleAppUI();
      gsaInterfacer.NewFile(false);

      var dir = TestDataDirectory;
      if (subdir != String.Empty)
      {
        dir = Path.Combine(TestDataDirectory, subdir);
        dir = dir + @"\"; // TestDataDirectory setup unconvetionally with trailing seperator - follow suit
      }

      var receiverProcessor = new ReceiverProcessor(dir, gsaInterfacer, gsaCache);

      // Run conversion to GWA keywords
      // Note that it can be one model split over several json files
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(jsonFiles, out var gwaRecordsFromFile, layer);

      //Run conversion to GWA keywords
      Assert.IsNotNull(gwaRecordsFromFile);
      Assert.IsNotEmpty(gwaRecordsFromFile);

      var designTypeHierarchy = Helper.GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Design, false);
      var analysisTypeHierarchy = Helper.GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Analysis, false);
      var keywords = designTypeHierarchy.Select(i => i.Key.GetGSAKeyword()).ToList();
      keywords.AddRange(designTypeHierarchy.SelectMany(i => i.Key.GetSubGSAKeyword()));
      keywords.AddRange(analysisTypeHierarchy.Select(i => i.Key.GetGSAKeyword()));
      keywords.AddRange(analysisTypeHierarchy.SelectMany(i => i.Key.GetSubGSAKeyword()));
      keywords = keywords.Where(k => k.Length > 0).Distinct().ToList();

      Initialiser.Interface.Sync(); // send GWA to GSA

      var retrievedGwa = Initialiser.Interface.GetGwaData(keywords, true); // read GWA from GSA

      var retrievedDict = new Dictionary<string, List<string>>();
      foreach (var gwa in retrievedGwa)
      {
        Initialiser.Interface.ParseGeneralGwa(gwa.GwaWithoutSet, out string keyword, out _, out _, out _, out _, out _);
        if (!retrievedDict.ContainsKey(keyword))
        {
          retrievedDict.Add(keyword, new List<string>());
        }
        retrievedDict[keyword].Add(gwa.GwaWithoutSet);
      }

      var fromFileDict = new Dictionary<string, List<string>>();
      foreach (var r in gwaRecordsFromFile)
      {
        Initialiser.Interface.ParseGeneralGwa(r.GwaCommand, out string keyword, out _, out _, out _, out _, out _);
        if (!fromFileDict.ContainsKey(keyword))
        {
          fromFileDict.Add(keyword, new List<string>());
        }
        fromFileDict[keyword].Add(r.GwaCommand);
      }

      Initialiser.Interface.Close();

      var unmatching = new Dictionary<string, UnmatchedData>();
      foreach (var keyword in fromFileDict.Keys)
      {
        if (!retrievedDict.ContainsKey(keyword))
        {
          unmatching[keyword] = new UnmatchedData();
          unmatching[keyword].FromFile = fromFileDict[keyword];
        }
        else if (retrievedDict[keyword].Count != fromFileDict[keyword].Count)
        {
          unmatching[keyword] = new UnmatchedData();
          unmatching[keyword].Retrieved = (retrievedDict.ContainsKey(keyword)) ? retrievedDict[keyword] : null;
          unmatching[keyword].FromFile = fromFileDict[keyword];
        }
      }

      Assert.AreEqual(0, unmatching.Count());

      // GSA sometimes forgets the SID - should check that this has passed through correctly here
    }
  }
}
