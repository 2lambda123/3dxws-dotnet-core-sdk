﻿// ------------------------------------------------------------------------------------------------------------------------------------
// Copyright 2022 Dassault Systèmes - CPE EMED
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
// ------------------------------------------------------------------------------------------------------------------------------------

using ds.enovia.common.constants;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ds.enovia.common.helper
{
    public class PostHttpJsonMessage : HttpRequestMessage
    {
        public PostHttpJsonMessage(Uri _uri, string _jsonPayload = null) : base(HttpMethod.Post, _uri)
        {
            this.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(MimeTypes.APPLICATION_JSON));

            if (_jsonPayload != null)
                this.Content = new StringContent(_jsonPayload, Encoding.UTF8, MimeTypes.APPLICATION_JSON);
        }
    }
}
