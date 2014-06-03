﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure;
using Infrastructure.DataType;
using Infrastructure.Enum;
using Infrastructure.Interface;
using Infrastructure.ObjectModel;

namespace XmlDocumentWrapper
{
    public static class XmlDocumentHistoryComparer
    {
        public static void CreateHistoryTrace(XmlWorkflowItem before, XmlWorkflowItem after)
        {
            if(before.Equals(after))
            {
                before.HistoryStates.Add(HistoryState.I);
                after.HistoryStates.Add(HistoryState.I);
                return;
            }

            before.HistoryStates.Add(HistoryState.D);
            after.HistoryStates.Add(HistoryState.D);

            if(!before.ArePropertiesEqual(after))
            {
                before.HistoryStates.Add(HistoryState.P);
                after.HistoryStates.Add(HistoryState.P);

                List<XmlStringPropertyHistory> l = CreatePropertyChangeHistory(before, after);
                AddItems(before.ChangedProperties, l);
            }

            List<XmlWorkflowItem> beforeClone = new List<XmlWorkflowItem>();
            foreach(var child in before.Children)
            {
                beforeClone.Add(child);
            }

            List<XmlWorkflowItem> afterClone = new List<XmlWorkflowItem>();
            foreach(var child in after.Children)
            {
                afterClone.Add(child);
            }

            List<XmlWorkflowItem> removeListFromBefore = new List<XmlWorkflowItem>();
            List<XmlWorkflowItem> removeListFromAfter = new List<XmlWorkflowItem>();

            // Remove any entry that have L mark
            foreach(var item in beforeClone)
            {
                if(item.HistoryStates.Contains(HistoryState.L))
                {
                    removeListFromBefore.Add(item);
                }
            }
            foreach(var item in afterClone)
            {
                if(item.HistoryStates.Contains(HistoryState.L))
                {
                    removeListFromAfter.Add(item);
                }
            }
            RemoveItems(beforeClone, removeListFromBefore);
            removeListFromBefore.Clear();
            RemoveItems(afterClone, removeListFromAfter);
            removeListFromAfter.Clear();

            // Look for Removed
            foreach(var item in beforeClone)
            {
                if(!afterClone.ContainsItem(item.Id))
                {
                    removeListFromBefore.Add(item);
                    XmlWorkflowItem match = after.GetNode(item.Id);
                    if(match != null)
                    {
                        item.HistoryStates.Add(HistoryState.L);
                        match.HistoryStates.Add(HistoryState.L);
                        // Todo: multitask -> Can't. L state will have race condition
                        CreateHistoryTrace(item, match);
                    }
                    else
                    {
                        item.HistoryStates.Add(HistoryState.R);
                    }
                }
            }
            RemoveItems(beforeClone, removeListFromBefore);
            removeListFromBefore.Clear();

            // Look for Added
            foreach(var item in afterClone)
            {
                if(!beforeClone.ContainsItem(item.Id))
                {
                    removeListFromAfter.Add(item);
                    XmlWorkflowItem match = before.GetNode(item.Id);
                    if(match != null)
                    {
                        item.HistoryStates.Add(HistoryState.L);
                        match.HistoryStates.Add(HistoryState.L);
                        // Todo: multitask -> Can't. L state will have race condition
                        CreateHistoryTrace(match, item);
                    }
                    else
                    {
                        item.HistoryStates.Add(HistoryState.A);
                    }
                }
            }
            RemoveItems(afterClone, removeListFromAfter);
            removeListFromAfter.Clear();

            CheckItemMovedAround(beforeClone, afterClone);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        public static List<XmlStringPropertyHistory> CreatePropertyChangeHistory(XmlType before, XmlType after)
        {
            if(!before.TypeName.Equals(after.TypeName)) // Not gonna compare 2 different types. No thank you.
            {
                // Throw exception to let you know you made a dumb call
                throw new TypeMismatchException(@"Can't compare 2 different types.");
            }

            List<XmlStringPropertyHistory> ret = new List<XmlStringPropertyHistory>();

            foreach(var property in before.Properties)  // Traverse through properties
            {
                if(!after.Properties.Contains(property))
                {

                    XmlPropertyAbstract match = (from p in after.Properties
                                                 where p.Name.Equals(property.Name)
                                                 select p).FirstOrDefault();
                    // Todo: I don't want to deal with this now. So throw exception
                    if(match == null)
                    {
                        throw new NotImplementedException(@"Cannot deal with property disappeared yet.");
                    }
                    if(property.GetType() != match.GetType())   // NULL property case
                    {
                        if(property is XmlStringProperty && match is XmlTypeProperty)
                        {
                            XmlStringProperty mStringProperty = property as XmlStringProperty;
                            XmlTypeProperty mTypeProperty = match as XmlTypeProperty;
                            string newType = mTypeProperty.Value.ToString();
                            if(mStringProperty.Value.ToString().ToLower().Contains("null"))
                            {
                                XmlStringPropertyHistory sph = new XmlStringPropertyHistory
                                {
                                    OriginalProperty = property,
                                    OriginalValue = @"NULL",
                                    ChangedValue = String.Format(@"new {0}", newType)
                                };
                                ret.Add(sph);
                            }
                            else
                            {
                                throw new NotImplementedException(@"Cannot deal with this case yet.");
                            }
                        }
                        else if(property is XmlTypeProperty && match is XmlStringProperty)
                        {
                            XmlStringProperty mStringProperty = match as XmlStringProperty;
                            XmlTypeProperty mTypeProperty = property as XmlTypeProperty;
                            string oldType = mTypeProperty.Value.ToString();
                            if(mStringProperty.Value.ToString().ToLower().Contains("null"))
                            {
                                XmlStringPropertyHistory sph = new XmlStringPropertyHistory
                                {
                                    OriginalProperty = property,
                                    OriginalValue = String.Format(@"delete {0}", oldType),
                                    ChangedValue = @"NULL"
                                };
                                ret.Add(sph);
                            }
                            else
                            {
                                throw new NotImplementedException(@"Cannot deal with this case yet.");
                            }
                        }
                        else
                        {
                            throw new NotImplementedException(@"Cannot deal with this case yet.");
                        }
                    }
                    else if(property is XmlStringProperty)
                    {
                        XmlStringProperty mStringProperty = match as XmlStringProperty;
                        if(mStringProperty != null)
                        {
                            XmlStringPropertyHistory sph = new XmlStringPropertyHistory
                            {
                                OriginalProperty = property,
                                OriginalValue = (property as XmlStringProperty).Value.ToString(),
                                ChangedValue = mStringProperty.Value.ToString()
                            };
                            ret.Add(sph);
                        }
                    }
                    else if(property is XmlTypeProperty)
                    {
                        XmlTypeProperty mTypeProperty = match as XmlTypeProperty;
                        if(mTypeProperty != null)
                        {
                            List<XmlStringPropertyHistory> result = 
                                CreatePropertyChangeHistory(property.Value as XmlType, mTypeProperty.Value as XmlType);
                            AddItems(ret, result);
                        }

                        // Todo: deal with Array
                    }
                }
            }

            if(before is XmlDataItem)
            {
                // Have not found a way to compare DataItem completely yet.
                //  For example, DataItem is an Array of Points. There are many scenarios 
                //      where Array can be changed.
            }

            return ret;
        }

        /// <summary>
        /// 2 sets of identical workflow items (Id wise identical). Determine if any item was moved out of order
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        private static void CheckItemMovedAround(IList<XmlWorkflowItem> before, IList<XmlWorkflowItem> after)
        {
            if(before.Count != after.Count)
            {
                throw new Exception(@"Can't perform on 2 different set.");
            }

            if(before.Count == 0)
            {
                return;
            }

            if(before[0].Id.Equals(after[0].Id))    // Item is in place.
            {
                CreateHistoryTrace(before[0], after[0]);
                // Remove item from the list and check the other ones still in the set
                before.Remove(before[0]);
                after.Remove(after[0]);
                CheckItemMovedAround(before, after);
            }
            else
            {   // Item not in place, mark M, remove and call recursively
                XmlWorkflowItem match = after.GetNode(before[0].Id);
                if(match == null)
                {
                    throw new Exception(@"Can't perform on 2 different set.");
                }
                CreateHistoryTrace(before[0], match);
                before[0].HistoryStates.Add(HistoryState.M);
                match.HistoryStates.Add(HistoryState.M);
                before.Remove(before[0]);
                after.Remove(match);
                CheckItemMovedAround(before, after);
            }
        }

        private static XmlWorkflowItem GetNode(this IEnumerable<XmlWorkflowItem> list, string id)
        {
            foreach(var item in list)
            {
                if(item.Id.Equals(id))
                {
                    return item;
                }
            }
            return null;
        }

        private static void RemoveItems(IList from, IList at)
        {
            foreach(var item in at)
            {
                from.Remove(item);
            }
        }

        /// <summary>
        /// Add items from a list to the end of another list.
        /// </summary>
        /// <param name="to"></param>
        /// <param name="from"></param>
        private static void AddItems(IList to, IList from)
        {
            if(from == null)
            {
                return;
            }
            if(to == null)
            {
                to = from;
                return;
            }
            foreach(var item in from)
            {
                to.Add(item);
            }
        }

        private static bool ContainsItem(this IEnumerable<XmlWorkflowItem> list, string id)
        {
            foreach(var item in list)
            {
                if(item.Id.Equals(id))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
