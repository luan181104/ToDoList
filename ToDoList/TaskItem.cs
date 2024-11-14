using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToDoList
{
    public class TaskItem
    {
        
            public string? Description { get; set; }
            public Priority Priority { get; set; }
            public DateTime? DueDate { get; set; }
            public bool IsCompleted { get; set; }

            public TaskStatus Status { get; set; }

        public override string ToString()
            {
                return $"{Description} - {Priority} - {DueDate?.ToString("MM/dd/yyyy hh:mm:ss tt") ?? "N/A" } - {Status}";
            }
        }

        public enum Priority
        {
            High,
            Medium,
            Low
        }

        public enum TaskStatus
        {
            InProgress,
            Completed,
            Cancelled
        }
    
}
