﻿//------------------------------------------------------------------------------------------------------------------------------------
// Copyright 2020 Dassault Systèmes - CPE EMED
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


using ds.enovia.common.constants;
using System;

namespace ds.authentication.redirection
{
    public class UserInfoRedirection : IPassportServiceRedirection
    {
        string m_tenant = null;
        string m_enoviaService = null;

        public const string RESOURCE_EP = "resources/modeler/pno/person";

        public UserInfoRedirection(string _enoviaService, string _tenant = null)
        {
            if (!_enoviaService.EndsWith(@"/"))
            {
                _enoviaService += @"/";
            }

            m_enoviaService = _enoviaService;
            m_tenant = _tenant;

            Current = true;

            IncludeCollaborativeSpaces = false;
            IncludePreferredCredentials = false;
        }

        public bool Current
        {
            get; set;
        }
        public bool IncludeCollaborativeSpaces
        {
            get; set;
        }

        public bool IncludePreferredCredentials
        {
            get; set;
        }

        #region ISserviceRedirection implementation interface
        public string GetServiceURL()
        {
            string __serviceURL = string.Format("{0}{1}", m_enoviaService, RESOURCE_EP);

            string serviceParams = "";

            if (Current)
            {
                serviceParams += "&current=true";
            }

            if (IncludeCollaborativeSpaces)
            {
                serviceParams += "&select=collabspaces";
            }

            if (IncludePreferredCredentials)
            {
                serviceParams += "&select=preferredcredentials";
            }

            if (m_tenant != null)
            {
                serviceParams += string.Format("&{0}={1}", HttpRequestParams.TENANT, m_tenant);
            }

            if ((serviceParams.Length > 0) && (serviceParams.StartsWith("&")))
            {
                //remove initial & character
                serviceParams = serviceParams.Substring(1, serviceParams.Length - 1);
            }

            // __serviceURL += Uri.EscapeDataString(string.Format("?{0}", serviceParams));
            __serviceURL += string.Format("?{0}", serviceParams);

            return __serviceURL;
        }
        #endregion
    }
}
