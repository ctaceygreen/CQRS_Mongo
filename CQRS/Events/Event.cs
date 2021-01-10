using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CQRS.Events
{
    public class Event : Message
    {
        public int Version { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public Guid AggregateId { get; set; }
        public DateTime DateCreated { get; set; }

        public Event()
        {
            Type = GetType().FullName;
            DateCreated = DateTime.UtcNow;
        }
    }
}
