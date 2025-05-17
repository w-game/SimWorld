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

        public Work SelfJob => Data[0] as Work;
        public Dictionary<JobUnitType, List<JobUnit>> JobUnits
        {
            get
            {
                Dictionary<JobUnitType, List<JobUnit>> jobUnits = new Dictionary<JobUnitType, List<JobUnit>>();
                foreach (var jobUnit in SelfJob.Property.JobBoard.JobUnits)
                {
                    if (SelfJob.ExpectJobUnits.Contains(jobUnit.Key))
                    {
                        if (!jobUnits.ContainsKey(jobUnit.Key))
                        {
                            jobUnits.Add(jobUnit.Key, new List<JobUnit>());
                        }
                        jobUnits[jobUnit.Key].AddRange(jobUnit.Value);
                    }
                }
                return jobUnits;
            }
        }

        public bool DoJobUnit(JobUnitType type, JobUnit jobUnit)
        {
            return SelfJob.DoJobUnit(type, jobUnit);
        }
    }
}