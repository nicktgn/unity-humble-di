using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;


namespace LobstersUnited.HumbleDI.Tests {

    public class ProviderAttributeTest {
        
        interface IFace { }

        class IFaceImpl : IFace { }

        [Provider(typeof(IFace))]
        class NotAProvider : MonoBehaviour { }
        
        [Provider(typeof(IFace))]
        class ZeroLevelProvider : MonoBehaviour, IFace { }

        [Provider(typeof(IFace))]
        class FirstLevelProvider : MonoBehaviour {
            IFace privateIFaceField;
            public IFace publicIFaceField_WontFind;
            protected IFace protectedIFaceField_WontFind;
        }

        [Provider(typeof(IFace))]
        class FirstLevelProviderSpecific : MonoBehaviour {
            IFaceImpl privateSpecificField;
        }

        [Serializable]
        class InnerProvider {
            IFace privateIFaceField;
            public IFace publicIFaceField_WontFind;
            protected IFace protectedIFaceField_WontFind;
        }

        [Provider(typeof(IFace))]
        class SecondLevelProviderUnreachable : MonoBehaviour {
            InnerProvider innerProvider;
        }
        
        [Provider(typeof(IFace), 2)]
        class SecondLevelProvider : MonoBehaviour {
            InnerProvider innerProvider;
        }

        public static IEnumerable<string> FIELD_NAME_LIST = new[] { "publicIFaceField", "privateIFaceField", "protectedIFaceField" };
        
        // ------------------------------- //

        [Test]
        public void should_fail_to_find_path_when_type_is_not_a_provider() {
            var targetType = typeof(NotAProvider);
            var attr = targetType.GetProviderAttributes().FirstOrDefault();
            
            var e = Assert.Throws<NotAProviderException>(() => attr.Find(targetType));
            
            Assert.That(e, Is.TypeOf<NotAProviderException>());
        }

        [Test]
        public void should_find_path_in_zero_level_provider() {
            var targetType = typeof(ZeroLevelProvider);
            var attr = targetType.GetProviderAttributes().FirstOrDefault();
            
            var path = attr.Find(targetType);
            
            Assert.That(path, Is.EqualTo(string.Empty));
        }

        [Test]
        public void should_find_path_in_first_level_provider() {
            var targetType = typeof(FirstLevelProvider);
            var attr = targetType.GetProviderAttributes().FirstOrDefault();

            var path = attr.Find(targetType);
            
            Assert.That(path, Is.EqualTo("privateIFaceField"));
        }

        [Test]
        public void should_find_path_in_first_level_provider_even_if_specific_field_used() {
            var targetType = typeof(FirstLevelProviderSpecific);
            var attr = targetType.GetProviderAttributes().FirstOrDefault();

            var path = attr.Find(targetType);
            
            Assert.That(path, Is.EqualTo("privateSpecificField"));
        }

        [Test]
        public void should_find_path_in_second_level_provider() {
            var targetType = typeof(SecondLevelProvider);
            var attr = targetType.GetProviderAttributes().FirstOrDefault();

            var path = attr.Find(targetType);
            
            Assert.That(path, Is.EqualTo("innerProvider.privateIFaceField"));
        }

        [Test]
        public void should_not_find_path_in_second_level_provider_if_max_level_is_lower() {
            
        }
    }
}
