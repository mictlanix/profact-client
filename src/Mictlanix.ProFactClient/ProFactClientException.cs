﻿//
// ProFactClientException.cs
//
// Author:
//       Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (c) 2016-2017 Mictlanix SAS de CV and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Mictlanix.CFDLib;

namespace Mictlanix.ProFact.Client {
	public class ProFactClientException : CFDException {
		public ProFactClientException ()
		{
		}

		public ProFactClientException (string message) : base (message)
		{
		}

		public ProFactClientException (string message, Exception innerException) : base (message, innerException)
		{
		}

		internal ProFactClientException (string code, string message) : base (message)
		{
			Code = code;
		}

		public string Code { get; private set; }

		public override string ToString()
		{
			return string.Format(string.IsNullOrWhiteSpace (Code) ? "{0}" : "{0} (Code: {1}).", Message, Code);
		}
	}
}
