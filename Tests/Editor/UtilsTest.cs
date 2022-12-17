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
        
    }
}
