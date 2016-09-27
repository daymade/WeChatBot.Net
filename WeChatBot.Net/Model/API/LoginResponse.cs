using System;

namespace WeChatBot.Net.Model.API
{
    /// <remarks/>
    [Serializable()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "error")]
    public partial class LoginResponse
    {

        private byte retField;

        private object messageField;

        private string skeyField;

        private string wxsidField;

        private int wxuinField;

        private string pass_ticketField;

        private byte isgrayscaleField;

        /// <remarks/>
        public byte ret
        {
            get
            {
                return this.retField;
            }
            set
            {
                this.retField = value;
            }
        }

        /// <remarks/>
        public object message
        {
            get
            {
                return this.messageField;
            }
            set
            {
                this.messageField = value;
            }
        }

        /// <remarks/>
        public string skey
        {
            get
            {
                return this.skeyField;
            }
            set
            {
                this.skeyField = value;
            }
        }

        /// <remarks/>
        public string wxsid
        {
            get
            {
                return this.wxsidField;
            }
            set
            {
                this.wxsidField = value;
            }
        }

        /// <remarks/>
        public int wxuin
        {
            get
            {
                return this.wxuinField;
            }
            set
            {
                this.wxuinField = value;
            }
        }

        /// <remarks/>
        public string pass_ticket
        {
            get
            {
                return this.pass_ticketField;
            }
            set
            {
                this.pass_ticketField = value;
            }
        }

        /// <remarks/>
        public byte isgrayscale
        {
            get
            {
                return this.isgrayscaleField;
            }
            set
            {
                this.isgrayscaleField = value;
            }
        }
    }
}