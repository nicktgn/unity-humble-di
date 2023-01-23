using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;


namespace LobstersUnited.HumbleDI.Tests {

    public class UtilsTest {
        
        public interface IFaceOne { }

        public interface IFaceTwo { }
        
        public class MBWithIFaceFields : MonoBehaviour {
            public IFaceOne publicIFaceField;
            IFaceOne privateIFaceField;
            protected IFaceTwo protectedIFaceField;
            
            public CharacterController nonIFaceField;
        }

        public static IEnumerable<string> FIELD_LIST = new[] { "publicIFaceField", "privateIFaceField", "protectedIFaceField" };

        
        [Test]
        public void should_find_all_interface_fields() {
            var cmp = A.Component<MBWithIFaceFields>();

            var fields = cmp.GetType().GetInterfaceFields();

            var fieldNames = fields.Select(f => f.Name);
            Assert.That(fieldNames, Is.EquivalentTo(FIELD_LIST));
        }

        public class GetFieldsWithAttributes_Tests {

            class FirstAttrAttribute : Attribute { }

            class SecondAttrAttribute : Attribute { }

            class TestClass {
                
                float privateFieldNone;
                protected float protectedFieldNone;
                public float publicFieldNone;
                
                [FirstAttr]
                float privateFieldOne;
                [FirstAttr]
                protected float protectedFieldOne;
                [FirstAttr]
                public float publicFieldOne;
                
                [FirstAttr, SecondAttr]
                float privateFieldTwo;
                [FirstAttr, SecondAttr]
                protected float protectedFieldTwo;
                [FirstAttr, SecondAttr]
                public float publicFieldTwo;
            }

            public static IEnumerable<string> FIRST_ATTR_LIST = new[] {
                "privateFieldOne", "protectedFieldOne", "publicFieldOne",
                "privateFieldTwo", "protectedFieldTwo", "publicFieldTwo",
            };
            
            public static IEnumerable<string> SECOND_ATTR_LIST = new[] {
                "privateFieldTwo", "protectedFieldTwo", "publicFieldTwo",
            };
            
            [Test]
            public void should_find_all_fields_that_have_specified_attribute() {
                var test = new TestClass();

                var fields = test.GetType().GetFieldsWithAttribute(typeof(FirstAttrAttribute));

                var fieldNames = fields.Select(f => f.Name);
                Assert.That(fieldNames, Is.EquivalentTo(FIRST_ATTR_LIST));
            }
            
            [Test]
            public void should_find_all_fields_that_have_another_specified_attribute() {
                var test = new TestClass();

                var fields = test.GetType().GetFieldsWithAttribute(typeof(SecondAttrAttribute));

                var fieldNames = fields.Select(f => f.Name);
                Assert.That(fieldNames, Is.EquivalentTo(SECOND_ATTR_LIST));
            }
        }
    }
}
