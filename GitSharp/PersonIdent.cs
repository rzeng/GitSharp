﻿/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Util;

namespace GitSharp
{
    public class PersonIdent
    {
        public string Name { get; private set; }
        public string EmailAddress { get; private set; }
        
        /// <summary>
        /// Elapsed milliseconds since Epoch (1970.1.1 00:00:00 GMT)
        /// </summary>
        public long When { get; private set; } // local date time in milliseconds (since Epoch)
        private readonly int tzOffset; // offset in minutes to UTC

        public PersonIdent(Repository repo)
        {
            RepositoryConfig config = repo.Config;
            string username = config.getString("user", null, "name");
            string email = config.getString("user", null, "email");
            Name = username;
            EmailAddress = email;
            DateTimeOffset now = DateTimeOffset.Now;
            When = now.Millisecond;
            tzOffset = now.Offset.Minutes;
        }

        public PersonIdent(PersonIdent pi) :
            this(pi.Name, pi.EmailAddress) 
        {}

        public PersonIdent(string name, string emailAddress) :
            this(name, emailAddress, DateTime.Now, TimeZoneInfo.Local)
        {}

        public PersonIdent(PersonIdent pi, DateTime when, TimeZoneInfo tz) :
            this(pi.Name, pi.EmailAddress, when, tz)
        {}

        public PersonIdent(PersonIdent pi, DateTime when) :
            this(pi.Name, pi.EmailAddress, when.ToUnixTime() * 1000, pi.tzOffset)
        {
        }

        public PersonIdent(string name, string emailAddress, DateTime when, TimeZoneInfo tz) :
            this (name, emailAddress, when.ToUnixTime() * 1000, tz.GetUtcOffset(when).Minutes)
        { }

        public PersonIdent(string name, string emailAddress, long when, int tz)
        {
            Name = name;
            EmailAddress = emailAddress;
            When = when;
            tzOffset = tz;
        }

        public PersonIdent(PersonIdent pi, long git_time, int offset_in_minutes) : this(pi.Name, pi.EmailAddress, git_time, offset_in_minutes)
        {}

        public PersonIdent(string str)
        {
            int lt = str.IndexOf('<');
            if (lt == -1)
            {
                throw new ArgumentException("Malformed PersonIdent string"
                        + " (no < was found): " + str);
            }
            int gt = str.IndexOf('>', lt);
            if (gt == -1)
            {
                throw new ArgumentException("Malformed PersonIdent string"
                        + " (no > was found): " + str);
            }
            int sp = str.IndexOf(' ', gt + 2);
            if (sp == -1)
            {
                When = 0;
                tzOffset = -1;
            }
            else
            {
                string tzHoursStr = str.Slice(sp + 1, sp + 4).Trim();
                int tzHours;
                if (tzHoursStr[0] == '+')
                {
                    tzHours = int.Parse(tzHoursStr.Substring(1));
                }
                else
                {
                    tzHours = int.Parse(tzHoursStr);
                }
                int tzMins = int.Parse(str.Substring(sp + 4).Trim());
                When = long.Parse(str.Slice(gt + 1, sp).Trim()) * 1000;
                tzOffset = tzHours * 60 + tzMins;
            }

            Name = str.Slice(0, lt).Trim();
            EmailAddress = str.Slice(lt + 1, gt).Trim();
        }

        /// <summary>
        /// TimeZone offset in minutes
        /// </summary>
        public int TimeZoneOffset
        {
            get
            {
                return tzOffset;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return EmailAddress.GetHashCode() ^ (int)When;
            }
        }

        public override bool Equals(object o)
        {
            var p = o as PersonIdent;
            if (p == null)
                return false;

            return Name == p.Name
                && EmailAddress == p.EmailAddress
                && When == p.When;
        }

        public string ToExternalString()
        {
            var r = new StringBuilder();

            r.Append(Name);
            r.Append(" <");
            r.Append(EmailAddress);
            r.Append("> ");
            r.Append(When / 1000);
            r.Append(' ');
            appendTimezone(r);
           
            return r.ToString();
        }

        private void appendTimezone(StringBuilder r)
        {
            int offset = tzOffset;
            char sign;
            int offsetHours;
            int offsetMins;

            if (offset < 0)
            {
                sign = '-';
                offset = -offset;
            }
            else
            {
                sign = '+';
            }

            offsetHours = offset / 60;
            offsetMins = offset % 60;

            r.Append(sign);
            if (offsetHours < 10)
            {
                r.Append('0');
            }
            r.Append(offsetHours);
            if (offsetMins < 10)
            {
                r.Append('0');
            }
            r.Append(offsetMins);
        }

        public override string ToString()
        {
#warning : to be finalized
		var r = new StringBuilder();
        //SimpleDateFormat dtfmt;
        //dtfmt = new SimpleDateFormat("EEE MMM d HH:mm:ss yyyy Z", Locale.US);
        //dtfmt.setTimeZone(getTimeZone());

		r.Append("PersonIdent[");
        r.Append(Name);
        r.Append(", ");
        r.Append(EmailAddress);
        r.Append(", ");
        //r.Append(dtfmt.format(Long.valueOf(when)));
        r.Append("]");

		return r.ToString();
        }
    }
}
