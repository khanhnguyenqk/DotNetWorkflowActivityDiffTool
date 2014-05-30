﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Infrastructure.Attribute;
using Infrastructure.ObjectModel;

namespace Infrastructure.DataType
{
    public class XmlItem : XmlType, IEquatable<XmlItem>
    {
        private string id = String.Empty;
        [NotNullable]
        public string Id
        {
            get { return id; }
            set
            {
                if(value != null && !value.Equals(id))
                {
                    id = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private ObservarableHashSet<XmlItem> children;

        public bool Equals(XmlItem other)
        {
            if(ReferenceEquals(null, other)) return false;
            if(ReferenceEquals(this, other)) return true;
            return base.Equals(other) && string.Equals(id, other.id) && children.Equals(other.children);
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(null, obj)) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj.GetType() != this.GetType()) return false;
            return Equals((XmlItem) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ id.GetHashCode();
                hashCode = (hashCode*397) ^ children.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(XmlItem left, XmlItem right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(XmlItem left, XmlItem right)
        {
            return !Equals(left, right);
        }
    }
}
