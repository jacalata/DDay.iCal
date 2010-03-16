﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace DDay.iCal
{
    public abstract class Evaluator :
        IEvaluator
    {
        #region Private Fields

        private System.Globalization.Calendar m_Calendar;
        private IList<IDateTime> m_StaticOccurrences;
        private IDateTime m_EvaluationStartBounds;
        private IDateTime m_EvaluationEndBounds;
        
        private ICalendarObject m_AssociatedObject;
        private ICalendarDataType m_AssociatedDataType;

        #endregion

        #region Protected Fields

        protected List<IPeriod> m_Periods;

        #endregion

        #region Constructors

        public Evaluator()
        {
            Initialize();
        }

        public Evaluator(ICalendarObject associatedObject)
        {
            m_AssociatedObject = associatedObject;

            Initialize();
        }

        public Evaluator(ICalendarDataType dataType)
        {
            m_AssociatedDataType = dataType;

            Initialize();
        }

        void Initialize()
        {
            m_Calendar = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            m_StaticOccurrences = new List<IDateTime>();
            m_Periods = new List<IPeriod>();
        }

        #endregion

        #region Protected Methods

        protected void IncrementDate(ref IDateTime dt, IRecurrencePattern pattern, int interval)
        {
            IDateTime old = dt;
            switch (pattern.Frequency)
            {
                case FrequencyType.Secondly: dt = old.AddSeconds(interval); break;
                case FrequencyType.Minutely: dt = old.AddMinutes(interval); break;
                case FrequencyType.Hourly: dt = old.AddHours(interval); break;
                case FrequencyType.Daily: dt = old.AddDays(interval); break;
                case FrequencyType.Weekly:
                    // How the week increments depends on the WKST indicated (defaults to Monday)
                    // So, basically, we determine the week of year using the necessary rules,
                    // and we increment the day until the week number matches our "goal" week number.
                    // So, if the current week number is 36, and our Interval is 2, then our goal
                    // week number is 38.
                    // NOTE: fixes RRULE12 eval.
                    int current = Calendar.GetWeekOfYear(old.Value, System.Globalization.CalendarWeekRule.FirstFourDayWeek, pattern.WeekStart),
                        lastLastYear = Calendar.GetWeekOfYear(new DateTime(old.Year - 1, 12, 31, 0, 0, 0, DateTimeKind.Local), System.Globalization.CalendarWeekRule.FirstFourDayWeek, pattern.WeekStart),
                        last = Calendar.GetWeekOfYear(new DateTime(old.Year, 12, 31, 0, 0, 0, DateTimeKind.Local), System.Globalization.CalendarWeekRule.FirstFourDayWeek, pattern.WeekStart),
                        goal = current + interval;

                    // If the goal week is greater than the last week of the year, wrap it!
                    if (goal > last)
                        goal = goal - last;
                    else if (goal <= 0)
                        goal = lastLastYear + goal;

                    int i = interval > 0 ? 1 : -1;
                    while (current != goal)
                    {
                        old = old.AddDays(i);
                        current = Calendar.GetWeekOfYear(old.Value, CalendarWeekRule.FirstFourDayWeek, pattern.WeekStart);
                    }

                    dt = old;
                    break;
                case FrequencyType.Monthly: dt = old.AddDays(-old.Day + 1).AddMonths(interval); break;
                case FrequencyType.Yearly: dt = old.AddDays(-old.DayOfYear + 1).AddYears(interval); break;
                default: throw new Exception("FrequencyType.NONE cannot be evaluated. Please specify a FrequencyType before evaluating the recurrence.");
            }
        }

        protected void Associate(IDateTime startDate, IDateTime fromDate, IDateTime toDate)
        {
            ICalendarObject associatedObject = AssociatedObject;
            if (associatedObject == null && 
                (
                    // Only throw an exception of one of the date/time values requires an associated
                    // object in order to evaluate properly.
                    startDate.TZID != null ||
                    fromDate.TZID != null ||
                    toDate.TZID != null
                ))
                throw new Exception("An error occurred during evaluation: Cannot associate the date/time values with a calendar.");

            startDate.AssociateWith(associatedObject);
            fromDate.AssociateWith(associatedObject);
            toDate.AssociateWith(associatedObject);
        }

        protected void Deassociate(IDateTime startDate, IDateTime fromDate, IDateTime toDate)
        {
            startDate.Deassociate();
            fromDate.Deassociate();
            toDate.Deassociate();
        }

        #endregion

        #region IEvaluator Members

        public System.Globalization.Calendar Calendar
        {
            get { return m_Calendar; }
        }

        public IList<IDateTime> StaticOccurrences
        {
            get { return m_StaticOccurrences; }
        }

        virtual public IDateTime EvaluationStartBounds
        {
            get { return m_EvaluationStartBounds; }
            set { m_EvaluationStartBounds = value; }
        }

        virtual public IDateTime EvaluationEndBounds
        {
            get { return m_EvaluationEndBounds; }
            set { m_EvaluationEndBounds = value; }
        }

        virtual public ICalendarObject AssociatedObject
        {
            get
            {
                if (m_AssociatedObject != null)
                    return m_AssociatedObject;
                else if (m_AssociatedDataType != null)
                    return m_AssociatedDataType.AssociatedObject;
                else
                    return null;
            }
            protected set { m_AssociatedObject = value; }
        }

        virtual public IList<IPeriod> Periods
        {
            get { return m_Periods; }
        }

        virtual public void Clear()
        {
            m_EvaluationStartBounds = null;
            m_EvaluationEndBounds = null;
            m_StaticOccurrences.Clear();
            m_Periods.Clear();
        }

        abstract public IList<IPeriod> Evaluate(
            IDateTime startDate,
            IDateTime fromDate,
            IDateTime toDate);

        #endregion
    }
}