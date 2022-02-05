﻿//------------------------------------------------------------------------------------------------------------------------------------
// Copyright 2021 Dassault Systèmes - CPE EMED
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify,
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
//  
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using System.Text.Json.Serialization;

namespace ds.enovia.common.model
{
    public class ItemPatchAttributes
    {
        //Standard properties
        private const string TYPE               = "type";
        private const string ID                 = "id";
        private const string TITLE              = "title";
        private const string DESCRIPTION        = "description";
        private const string CUSTOMERATTRIBUTES = "customerAttributes";

        private IDictionary<string, object> m_extensionDictionary = new Dictionary<string, object>();

        protected IDictionary<string, object> ExtensionDictionary => m_extensionDictionary;

        public ItemPatchAttributes()
        {
            this.ExtensionDictionary[CUSTOMERATTRIBUTES] = (new Dictionary<string, object>());
        }
       
        #region Properties
        [JsonPropertyName(TYPE)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Type { get; set; }

        [JsonPropertyName(ID)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; set; }

        [JsonPropertyName(TITLE)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Title { get;set; }

        [JsonPropertyName(DESCRIPTION)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }

        [JsonPropertyName(CUSTOMERATTRIBUTES)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string,object> customerAttributes { get; set; }

        #endregion Properties
    }
}
