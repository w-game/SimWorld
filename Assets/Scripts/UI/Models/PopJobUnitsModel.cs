using System;
using System.Collections.Generic;
using System.Linq;
using Citizens;
using UI.Views;
using Unity.VisualScripting;

namespace UI.Models
{
    public class PopJobUnitsModel : ModelBase<PopJobUnits>
    {
        public override string Path => "PopJobUnits";
        public override ViewType ViewType => ViewType.Popup;

        public Job Job => Data[0] as Job;
        public Dictionary<Type, List<JobUnit>> JobUnits
        {
            get
            {
                Dictionary<Type, List<JobUnit>> jobUnits = new Dictionary<Type, List<JobUnit>>();
                foreach (var jobUnit in Job.Property.JobUnits)
                {
                    if (jobUnit.Key == typeof(Job))
                    {
                        if (jobUnit.Value.Count > 0)
                        {
                            jobUnits.Add(jobUnit.Key, jobUnit.Value);
                        }
                    }
                }

                if (jobUnits.Count == 0 && Job is Owner)
                {
                    return Job.Property.JobUnits;
                }
                return jobUnits;
            }
        }

        public void DoJobUnit(Type type, JobUnit jobUnit)
        {
            Job.DoJobUnit(type, jobUnit);
        }
    }
}