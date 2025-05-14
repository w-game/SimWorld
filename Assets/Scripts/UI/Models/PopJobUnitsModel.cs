using System;
using System.Collections.Generic;
using Citizens;
using UI.Views;

namespace UI.Models
{
    public class PopJobUnitsModel : ModelBase<PopJobUnits>
    {
        public override string Path => "PopJobUnits";
        public override ViewType ViewType => ViewType.Popup;

        public Job SelfJob => Data[0] as Job;
        public Dictionary<Type, List<JobUnit>> JobUnits
        {
            get
            {
                Dictionary<Type, List<JobUnit>> jobUnits = new Dictionary<Type, List<JobUnit>>();
                foreach (var jobUnit in SelfJob.Property.JobUnits)
                {
                    if (jobUnit.Key == SelfJob.GetType())
                    {
                        if (jobUnit.Value.Count > 0)
                        {
                            jobUnits.Add(jobUnit.Key, jobUnit.Value);
                        }
                    }
                }

                if (jobUnits.Count == 0 &&( SelfJob is Owner || SelfJob is Rentant))
                {
                    return SelfJob.Property.JobUnits;
                }
                return jobUnits;
            }
        }

        public bool DoJobUnit(Type type, JobUnit jobUnit)
        {
            return SelfJob.DoJobUnit(type, jobUnit);
        }
    }
}