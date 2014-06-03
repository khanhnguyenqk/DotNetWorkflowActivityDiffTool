﻿using System;
using System.Xml;
using Infrastructure.Attribute;
using Infrastructure.Interface;
using Infrastructure.ObjectModel;

namespace Infrastructure.DataType
{
    public class XmlDataItem : XmlType, IEquatable<XmlDataItem>, IPropertyValue
    {
        /// <summary>
        /// Metadata this is not used in object comparison.
        /// </summary>
        public XmlDataItem Parent { get; set; }

        public XmlPropertyAbstract PropertyHost { get; set; }

        private ObservableList<XmlDataItem> children = new ObservableList<XmlDataItem>();
        [NotNullable]
        public ObservableList<XmlDataItem> Children
        {
            get { return children; }
            set
            {
                if(value != null && value != children)
                {
                    children = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public XmlDataItem(XmlNode xmlNode) : base(xmlNode)
        {
            Parent = null;

            if(String.IsNullOrEmpty(xmlNode.InnerText) || !xmlNode.InnerXml.Equals(xmlNode.InnerText))
            {
                foreach(XmlNode node in xmlNode.ChildNodes)
                {
                    string parentNode, nodeName;
                    if(!XmlParserHelper.IsAProperty(node, out parentNode, out nodeName))
                    {
                        XmlDataItem di = new XmlDataItem(node, this);
                        children.Add(di);
                    }
                }
            }
        }

        public XmlDataItem(XmlNode xmlNode, XmlPropertyAbstract propertyHost):this(xmlNode)
        {
            PropertyHost = propertyHost;
        }

        public XmlDataItem(XmlNode xmlNode, XmlDataItem parent) : this(xmlNode)
        {
            Parent = parent;
        }

        public bool Equals(XmlDataItem other)
        {
            if(ReferenceEquals(null, other)) return false;
            if(ReferenceEquals(this, other)) return true;
            return base.Equals(other) && children.Equals(other.children);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((XmlDataItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ children.GetHashCode();
            }
        }

        public static bool operator ==(XmlDataItem left, XmlDataItem right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(XmlDataItem left, XmlDataItem right)
        {
            return !Equals(left, right);
        }
    }
}
