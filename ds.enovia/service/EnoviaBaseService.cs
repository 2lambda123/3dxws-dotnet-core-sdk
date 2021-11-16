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

using ds.authentication;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ds.enovia.service
{
    public class EnoviaBaseService
    {
        //Rest connection
        protected HttpClient m_client = null;
        private HttpClientHandler m_clientHandler = new HttpClientHandler();

        private IPassportAuthentication m_authentication = null;

        private Uri m_enoviaHost = null;
        private string m_enoviaService = null;

        //ENO_CSRF_TOKEN
        CsrfTokenCache m_tokenCache = null;
        private const int CSRF_CACHE_INTERVAL = 55; //TODO Configuration. Currently hardcoded, use the CSRF TOKEN for 55 minutes.

        // -- PnO Resources --
        private const string CSRF_TOKEN = "/resources/v1/application/CSRF";

        public string SecurityContext { get; set; }

        //For cloud
        public string Tenant { get; set; }

        public EnoviaBaseService(string enoviaService, IPassportAuthentication _passport)
        {
            m_authentication = _passport;

            Uri enoviaUri = new Uri(enoviaService);

            m_enoviaService = enoviaUri.LocalPath;

            string enoviaHost = string.Format("{0}://{1}", enoviaUri.Scheme, enoviaUri.Host);

            m_enoviaHost = new Uri(enoviaHost);

            // Initialize RestClient and Cookie Manager if Cookie Authentication
            m_client = new HttpClient(m_clientHandler) { BaseAddress = m_enoviaHost };

            if (Authentication.IsCookieAuthentication())
            {
                //..........
                m_clientHandler.CookieContainer = Authentication.GetCookieContainer();
            }
            else
            {
                m_clientHandler.CookieContainer = new CookieContainer();
            }

        }

        protected HttpClientHandler BaseClientHandler
        {
            get { return m_clientHandler; }
        }

        public string EnoviaServiceURL { get { return string.Format("{0}{1}", m_enoviaHost.ToString(), m_enoviaService.ToString()); } }

        public IPassportAuthentication Authentication { get { return m_authentication; } }

        public bool IncludeTenant
        {
            get
            {
                return (Tenant == null) ? false : true;
            }
        }

        protected string GetEndpointURL(string _endpoint)
        {
            if (m_enoviaService.EndsWith(@"/") && _endpoint.StartsWith(@"/"))
            {
                _endpoint = _endpoint.Substring(1, _endpoint.Length - 1);
            }

            return string.Format("{0}{1}", m_enoviaService, _endpoint);
        }

        #region CSRF Token Cache Management
        public async Task<string> GetCSRFToken(bool _useCache = true)
        {
            if (!_useCache || !IsTokenCacheValid())
            {
                //Refresh token cache
                m_tokenCache = await GetNewTokenCache();
            }

            return m_tokenCache.csrf.value;
        }

        private bool IsTokenCacheValid()
        {
            return _IsTokenCacheValid(m_tokenCache);
        }
        private bool _IsTokenCacheValid(CsrfTokenCache _cache)
        {
            if (_cache == null) return false;

            if (_cache.csrf == null) return false;

            System.TimeSpan timeInterval = (DateTime.Now - _cache.received);

            if (timeInterval.TotalMinutes > CSRF_CACHE_INTERVAL)
            {
                return false;
            }

            return true;
        }

        private async Task<CsrfTokenCache> GetNewTokenCache()
        {
            HttpResponseMessage tokenResponse = await GetAsync(CSRF_TOKEN);

            if (tokenResponse.StatusCode != HttpStatusCode.OK)
                throw new Exception(String.Format("Error getting 3DSpace CSRF token ({0}) ", tokenResponse.StatusCode));

            CsrfTokenResponse csrfTokenResponse = await tokenResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();

            CsrfTokenCache tokenCache = new CsrfTokenCache();
            tokenCache.received = DateTime.Now;
            tokenCache.csrf = csrfTokenResponse.csrf;

            return tokenCache;
        }

        #endregion

        public async Task<HttpResponseMessage> GetAsync(string _endpoint, IDictionary<string, string> _queryParameters = null, IDictionary<string, string> _headers = null, bool _requiresCsrfToken = false, bool _useCsrfCache = true)
        {
            return await ExecuteAsyncMethod(HttpMethod.Get, _endpoint, _queryParameters, _headers, null, true, _requiresCsrfToken, _useCsrfCache);
        }

        public async Task<HttpResponseMessage> PostAsync(string _endpoint, IDictionary<string, string> _queryParameters = null, IDictionary<string, string> _headers = null, string _body = null, bool _bodyIsJson = true, bool _requiresCsrfToken = true, bool _useCsrfCache = true)
        {
            return await ExecuteAsyncMethod(HttpMethod.Post, _endpoint, _queryParameters, _headers, _body, _bodyIsJson, _requiresCsrfToken, _useCsrfCache);
        }

        public async Task<HttpResponseMessage> PatchAsync(string _endpoint, IDictionary<string, string> _queryParameters = null, IDictionary<string, string> _headers = null, string _body = null, bool _bodyIsJson = true, bool _requiresCsrfToken = true, bool _useCsrfCache = true)
        {
            return await ExecuteAsyncMethod(new HttpMethod("PATCH"), _endpoint, _queryParameters, _headers, _body, _bodyIsJson, _requiresCsrfToken, _useCsrfCache);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string _endpoint, IDictionary<string, string> _queryParameters = null, IDictionary<string, string> _headers = null, bool _requiresCsrfToken = true, bool _useCsrfCache = true)
        {
            return await ExecuteAsyncMethod(HttpMethod.Delete, _endpoint, _queryParameters, _headers, null, false, _requiresCsrfToken, _useCsrfCache);
        }

        public async Task<HttpResponseMessage> PutAsync(string _endpoint, IDictionary<string, string> _queryParameters = null, IDictionary<string, string> _headers = null, string _body = null, bool _bodyIsJson = true, bool _requiresCsrfToken = true, bool _useCsrfCache = true)
        {
            return await ExecuteAsyncMethod(HttpMethod.Put, _endpoint, _queryParameters, _headers, _body, _bodyIsJson, _requiresCsrfToken, _useCsrfCache);
        }

        private async Task<HttpResponseMessage> ExecuteAsyncMethod(HttpMethod _method, string _endpoint, IDictionary<string, string> _queryParameters = null, IDictionary<string, string> _headers = null, string _body = null, bool _bodyIsJson = true, bool _requiresCsrfToken = true, bool _useCsrfCache = true)
        {
            EnoviaJsonRequest __request = await CreateJsonRequest(_method, _endpoint, IncludeTenant, _requiresCsrfToken, _useCsrfCache);

            if (_queryParameters != null)
            {
                foreach (string keyName in _queryParameters.Keys)
                {
                    __request.AddQueryParameter(keyName, _queryParameters[keyName]);
                }
            }

            if (_headers != null)
                __request.AddHeaders(_headers);

            if (_body != null)
            {
                __request.AddJsonPayload(_body);
            }

            return await m_client.SendAsync(__request);
        }

        private async Task<EnoviaJsonRequest> CreateJsonRequest(HttpMethod _httpMethod, string _endpoint, bool _includeTenant = false, bool _requiresCsrfToken = false, bool _useCsrfCache = true)
        {
            string csrfToken = null;

            if (_requiresCsrfToken)
            {
                csrfToken = await GetCSRFToken(_useCsrfCache);
            }

            EnoviaJsonRequest request = new EnoviaJsonRequest(_httpMethod, GetEndpointURL(_endpoint), Tenant, SecurityContext, csrfToken);

            if (!Authentication.IsCookieAuthentication())
            {
                Authentication.AuthenticateRequest(request);
            }

            return request;
        }
    }
}
