using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;


namespace LobstersUnited.HumbleDI.Tests {

    class CollectionWrapperTest {

        interface IItem { }

        class Item : IItem { }

        class TestClass {
            public IItem[] array;
            public List<IItem> list;
        }

        static readonly Type testClassType = typeof(TestClass);
        static readonly FieldInfo arrayField = testClassType.GetField("array");
        static readonly FieldInfo listField = testClassType.GetField("list");
        
        
        [Test]
        public void placeholder() { }

        class ArrayFieldWrapperTests {
            
            [Test]
            public void should_create_empty_array_field_wrapper() {
                var obj = new TestClass();

                var wrapper = new CollectionWrapper(arrayField, obj);

                Assert.That(obj.array, Is.Null);
                Assert.That(wrapper.Object, Is.Null);
                Assert.That(wrapper.IsArray, Is.True);
                Assert.That(wrapper.IsList, Is.False);
                Assert.That(wrapper.Count, Is.Zero);
            }
            
            [Test]
            public void should_create_array_field_wrapper_with_existing_array() {
                var item1 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1 },
                };
                
                var wrapper = new CollectionWrapper(arrayField, obj);

                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.IsArray, Is.True);
                Assert.That(wrapper.IsList, Is.False);
                Assert.That(wrapper.Count, Is.EqualTo(1));
            }

            [Test]
            public void should_create_array_and_add_element_to_array_field() {
                var obj = new TestClass();
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                var idx = wrapper.Add(null);
            
                Assert.That(idx, Is.EqualTo(0));
                Assert.That(wrapper.Object, Is.Not.Null);
                Assert.That(wrapper.AsArray.Length, Is.EqualTo(1));
                Assert.That(wrapper.Count, Is.EqualTo(1));
                Assert.That(obj.array, Is.SameAs(wrapper.Object));
            }
            
            [Test]
            public void should_add_element_to_existing_array_field() {
                var item = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                var idx = wrapper.Add(null);
            
                Assert.That(idx, Is.EqualTo(1));
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsArray, Is.EqualTo(new [] { item, null }));
            }
            
            [Test]
            public void should_remove_element_from_existing_array_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1, item2 },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                wrapper.RemoveAt(0);
                
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(1));
                Assert.That(wrapper.AsArray, Is.EqualTo(new [] { item2 }));
            }
            
            [Test]
            public void should_throw_exception_when_try_to_remove_index_that_is_out_of_range() {
                var item1 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1 },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.RemoveAt(2));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.RemoveAt(-1));
                
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(1));
                Assert.That(wrapper.AsArray, Is.EqualTo(new [] { item1 }));
            }
            
            [Test]
            public void should_throw_exception_if_try_to_remove_from_empty_array_field() {
                var obj = new TestClass {
                    array = Array.Empty<IItem>(),
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
                
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.RemoveAt(0));
                
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(0));
                Assert.That(wrapper.AsArray.Length, Is.EqualTo(0));
            }
            
            [Test]
            public void should_not_throw_anything_when_removing_from_null_array_field() {
                var obj = new TestClass();
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                wrapper.RemoveAt(0);
                
                Assert.That(wrapper.Object, Is.Null);
                Assert.That(obj.array, Is.Null);
                Assert.That(wrapper.Count, Is.EqualTo(0));
            }
            
            [Test]
            public void should_reorder_forward_element_in_array_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1, item2 },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                wrapper.Reorder(0,1);
                
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsArray, Is.EqualTo(new [] { item2, item1 }));
            }
            
            [Test]
            public void should_reorder_backward_element_in_array_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1, item2 },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                wrapper.Reorder(1,0);
                
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsArray, Is.EqualTo(new [] { item2, item1 }));
            }
            
            [Test]
            public void should_throw_if_reorder_to_index_out_of_range_in_array_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1, item2 },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);
            
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(-1,0));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(2,0));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(1,2));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(1,-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(2,-1));
                
                Assert.That(wrapper.Object, Is.SameAs(obj.array));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsArray, Is.EqualTo(new [] { item1, item2 }));
            }
        }
        
        // ------------------------------------- //

        class ListFieldWrapperTests {
            
            [Test]
            public void should_create_empty_list_field_wrapper() {
                var obj = new TestClass();

                var wrapper = new CollectionWrapper(listField, obj);

                Assert.That(obj.list, Is.Null);
                Assert.That(wrapper.Object, Is.Null);
                Assert.That(wrapper.IsArray, Is.False);
                Assert.That(wrapper.IsList, Is.True);
                Assert.That(wrapper.Count, Is.Zero);
            }
            
            [Test]
            public void should_create_list_field_wrapper_with_existing_list() {
                var item1 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1 },
                };
                
                var wrapper = new CollectionWrapper(listField, obj);

                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.IsArray, Is.False);
                Assert.That(wrapper.IsList, Is.True);
                Assert.That(wrapper.Count, Is.EqualTo(1));
            }

            [Test]
            public void should_create_list_and_add_element_to_list_field() {
                var obj = new TestClass();
                var wrapper = new CollectionWrapper(listField, obj);
            
                var idx = wrapper.Add(null);
            
                Assert.That(idx, Is.EqualTo(0));
                Assert.That(wrapper.Object, Is.Not.Null);
                Assert.That(wrapper.AsIList.Count, Is.EqualTo(1));
                Assert.That(wrapper.Count, Is.EqualTo(1));
                Assert.That(obj.list, Is.SameAs(wrapper.Object));
            }
            
            [Test]
            public void should_add_element_to_existing_list_field() {
                var item = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item },
                };
                var wrapper = new CollectionWrapper(listField, obj);
            
                var idx = wrapper.Add(null);
            
                Assert.That(idx, Is.EqualTo(1));
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsIList, Is.EqualTo(new List<IItem> { item, null }));
            }
            
            [Test]
            public void should_remove_element_from_existing_list_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1, item2 },
                };
                var wrapper = new CollectionWrapper(listField, obj);
            
                wrapper.RemoveAt(0);
                
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(1));
                Assert.That(wrapper.AsIList, Is.EqualTo(new List<IItem> { item2 }));
            }
            
            [Test]
            public void should_throw_exception_when_try_to_remove_index_that_is_out_of_range() {
                var item1 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1 },
                };
                var wrapper = new CollectionWrapper(listField, obj);
            
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.RemoveAt(2));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.RemoveAt(-1));
                
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(1));
                Assert.That(wrapper.AsIList, Is.EqualTo(new List<IItem> { item1 }));
            }
            
            [Test]
            public void should_throw_exception_if_try_to_remove_from_empty_list_field() {
                var obj = new TestClass {
                    list = new List<IItem>(),
                };
                var wrapper = new CollectionWrapper(listField, obj);
                
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.RemoveAt(0));
                
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(0));
                Assert.That(wrapper.AsIList.Count, Is.EqualTo(0));
            }
            
            [Test]
            public void should_not_throw_anything_when_removing_from_null_list_field() {
                var obj = new TestClass();
                var wrapper = new CollectionWrapper(listField, obj);
            
                wrapper.RemoveAt(0);
                
                Assert.That(wrapper.Object, Is.Null);
                Assert.That(obj.list, Is.Null);
                Assert.That(wrapper.Count, Is.EqualTo(0));
            }
            
            [Test]
            public void should_reorder_forward_element_in_list_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1, item2 },
                };
                var wrapper = new CollectionWrapper(listField, obj);
            
                wrapper.Reorder(0,1);
                
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsIList, Is.EqualTo(new List<IItem> { item2, item1 }));
            }
            
            [Test]
            public void should_reorder_backward_element_in_list_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1, item2 },
                };
                var wrapper = new CollectionWrapper(listField, obj);
            
                wrapper.Reorder(1,0);
                
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsIList, Is.EqualTo(new List<IItem> { item2, item1 }));
            }
            
            [Test]
            public void should_throw_if_reorder_to_index_out_of_range_in_list_field() {
                var item1 = new Item();
                var item2 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1, item2 },
                };
                var wrapper = new CollectionWrapper(listField, obj);
            
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(-1,0));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(2,0));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(1,2));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(1,-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => wrapper.Reorder(2,-1));
                
                Assert.That(wrapper.Object, Is.SameAs(obj.list));
                Assert.That(wrapper.Count, Is.EqualTo(2));
                Assert.That(wrapper.AsIList, Is.EqualTo(new List<IItem> { item1, item2 }));
            }
        }

        class IEnumerableImplementationTests {

            [Test]
            public void should_iterate_over_array_field() {
                var item1 = new Item();
                var obj = new TestClass {
                    array = new IItem[] { item1 },
                };
                var wrapper = new CollectionWrapper(arrayField, obj);

                var it = wrapper.Select(item => item);

                Assert.That(it, Is.EqualTo(new [] { item1 }));
            }
            
            [Test]
            public void should_iterate_over_empty_array_field() {
                var obj = new TestClass {
                    array = Array.Empty<IItem>(),
                };
                var wrapper = new CollectionWrapper(arrayField, obj);

                var it = wrapper.Select(item => item);

                Assert.That(it, Is.EqualTo(Array.Empty<IItem>()));
            }
            
            [Test]
            public void should_iterate_over_null_array_field() {
                var obj = new TestClass {
                    array = null,
                };
                var wrapper = new CollectionWrapper(arrayField, obj);

                var it = wrapper.Select(item => item);

                Assert.That(it, Is.EqualTo(Array.Empty<IItem>()));
            }
            
            [Test]
            public void should_iterate_over_list_field() {
                var item1 = new Item();
                var obj = new TestClass {
                    list = new List<IItem> { item1 },
                };
                var wrapper = new CollectionWrapper(listField, obj);

                var it = wrapper.Select(item => item);

                Assert.That(it, Is.EqualTo(new List<IItem> { item1 }));
            }
            
            [Test]
            public void should_iterate_over_empty_list_field() {
                var obj = new TestClass {
                    list = new List<IItem>(),
                };
                var wrapper = new CollectionWrapper(listField, obj);

                var it = wrapper.Select(item => item);

                Assert.That(it, Is.EqualTo(Array.Empty<IItem>()));
            }
            
            [Test]
            public void should_iterate_over_null_list_field() {
                var obj = new TestClass {
                    list = null,
                };
                var wrapper = new CollectionWrapper(listField, obj);

                var it = wrapper.Select(item => item);

                Assert.That(it, Is.EqualTo(Array.Empty<IItem>()));
            }
        }
    }
}
