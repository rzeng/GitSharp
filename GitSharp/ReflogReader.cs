﻿/*
 * Copyright (C) 2009, Robin Rosenberg
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
using System.IO;
using GitSharp.Util;

namespace GitSharp
{

    public class ReflogReader
    {
        public class Entry
        {
            private readonly ObjectId oldId;
            private readonly ObjectId newId;
            private readonly PersonIdent who;
            private readonly string comment;

            public Entry(byte[] raw, int pos)
            {
                oldId = ObjectId.FromString(raw, pos);
                pos += Constants.OBJECT_ID_LENGTH*2;
                if (raw[pos++] != ' ')
                    throw new ArgumentException("Raw log message does not parse as log entry");
                newId = ObjectId.FromString(raw, pos);
                pos += Constants.OBJECT_ID_LENGTH * 2;
                if (raw[pos++] != ' ')
                    throw new ArgumentException("Raw log message does not parse as log entry");
                who = RawParseUtils.parsePersonIdentOnly(raw, pos);
                int p0 = RawParseUtils.next(raw, pos, (byte)'\t');

                if (p0 == -1)
                    throw new ArgumentException("Raw log message does not parse as log entry");

                int p1 = RawParseUtils.nextLF(raw, p0);
                if (p1 == -1)
                    throw new ArgumentException("Raw log message does not parse as log entry");

                comment = RawParseUtils.decode(raw, p0, p1 - 1);
            }

            public ObjectId getOldId()
            {
                return oldId;
            }

            public ObjectId getNewId()
            {
                return newId;
            }

            public PersonIdent getWho()
            {
                return who;
            }

            public string getComment()
            {
                return comment;
            }

            public override string ToString()
            {
                return "Entry[" + oldId.Name + ", " + newId.Name + ", " + getWho() + ", " + getComment() + "]";
            }
        }

        private readonly FileInfo _logName;

        public ReflogReader(Repository db, string refname)
        {
            _logName = new FileInfo(Path.Combine(db.Directory.FullName, "logs/" + refname));
        }

        public Entry getLastEntry()
        {
            List<Entry> entries = getReverseEntries(1);
            return entries.Count > 0 ? entries[0] : null;
        }

        public List<Entry> getReverseEntries()
        {
            return getReverseEntries(int.MaxValue);
        }

        public List<Entry> getReverseEntries(int max)
        {
            byte[] log;
            
            if (!_logName.IsFile())
            {
                return new List<Entry>();
            }
            
            log = NB.ReadFully(_logName);

            int rs = RawParseUtils.prevLF(log, log.Length);
            var ret = new List<Entry>();
            while (rs >= 0 && max-- > 0)
            {
                rs = RawParseUtils.prevLF(log, rs);
                Entry entry = new Entry(log, rs < 0 ? 0 : rs + 2);
                ret.Add(entry);
            }
            return ret;
        }
    }

}