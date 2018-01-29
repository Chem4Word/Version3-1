using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chem4Word.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Chem4WordTests
{
    [TestClass]
    public class FunctionalGroupTests
    {
        [TestMethod]
       
        public void GetFunctionalGroups()
        {
            FunctionalGroup fg;
            FunctionalGroups.LoadDefaults();
            fg = FunctionalGroups.GetByName["CH3"];
            Assert.IsNotNull(fg);
        }

        [TestMethod]
        public void TestMultipleJSONLoad()
        {
            string groupJSON = @"
            [{
                'symbol':'CH2',
                'flippable':'false',
                'components':
                [
                    { 'element':'C'},
                    { 'element':'H', 'count':2}
                ],
                'showassymbol':false
            },
            {
                'symbol':'CH2CH2OH',
                'flippable':'true',
                'components':
                [
                    {'group':'CH2'},
                    {'group':'CH2'},
                    {'element':'O'},
                    {'element':'H'}
                ],
                'showassymbol':false
            }]";
           
            FunctionalGroups.Load(groupJSON);
            Assert.IsTrue(FunctionalGroups.GetByName.Count==2);
            GetJSONString();
        }
        [TestMethod]
        public void GetJSONString()
        {
            GetFunctionalGroups();
            string fgJSON = FunctionalGroups.GetJSON();
            Debug.Assert(!string.IsNullOrEmpty(fgJSON));
        }
    }
}
