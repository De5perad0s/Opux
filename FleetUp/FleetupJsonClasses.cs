using System;
using System.Collections.Generic;
using System.Text;

namespace FleetUp
{
    class FleetupJsonClasses
    {
        public class Result
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public DateTime CachedUntilUTC { get; set; }
            public string CachedUntilString { get; set; }
            public int Code { get; set; }
            public Operations[] Data { get; set; }
        }

        public class Operations
        {
            public int Id { get; set; }
            public int OperationId { get; set; }
            public string Subject { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string StartString { get; set; }
            public string EndString { get; set; }
            public string Location { get; set; }
            public int LocationId { get; set; }
            public string LocationInfo { get; set; }
            public string Details { get; set; }
            public string Url { get; set; }
            public string Organizer { get; set; }
            public string Category { get; set; }
            public string Group { get; set; }
            public int GroupId { get; set; }
            public object[] Doctrines { get; set; }
        }

    }
}
