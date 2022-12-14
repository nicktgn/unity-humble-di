using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;


namespace LobstersUnited.HumbleDI.Tests {

    public class TestUtils {
        
        public interface IFaceOne {
            public void one();
        }

        public interface IFaceTwo {
            public void two();
        }
        
        public class MBWithIFaceFields : MonoBehaviour {
            
            public IFaceOne publicIFaceField;

            IFaceOne privateIFaceField;

            protected IFaceTwo protectedIFaceField;

            public CharacterController nonIFaceField;
        }

        public static IEnumerable<string> IFACE_LIST = new[] { "publicIFaceField", "privateIFaceField", "protectedIFaceField" };

        // A Test behaves as an ordinary method
        [Test]
        public void should_find_all_interfaces_of_type() {
            var cmp = A.Component<MBWithIFaceFields>();

            var fields = cmp.GetType().GetInterfaceFields();

            var fieldNames = fields.Select(f => f.Name);
            Assert.That(fieldNames, Is.EquivalentTo(IFACE_LIST));
        }
        
    }
}
