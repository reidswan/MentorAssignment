using System;
using System.Collections.Generic;

namespace Scheduler
{
    class Scheduler
    {
        static void Main(string[] args)
        {
            List<Person> mentors = GeneratePeople(25, Role.Mentor, 60);
            List<Person> mentees = GeneratePeople(100, Role.Mentee, 70);

            List<Person> unassignedMentors = new List<Person>(mentors);
            List<Person> unassignedMentees = new List<Person>(mentees);
            
            // mentors assigned to periods
            Person[,] assignedPeriods = new Person[5,10];
            // mentees assigned to periods
            List<Person>[,] menteesAssigned = new List<Person>[5,10];
            for (int i = 0; i < 5; i++)
            {
                for(int j = 0; j < 10; j++)
                {
                    menteesAssigned[i, j] = new List<Person>();
                }
            }

            int failedLoopCount = 0;
            int uCountPrev = 0;
            while (unassignedMentees.Count > 0 && failedLoopCount < 15)
            {                
                // if any mentee can only be assigned to one mentor, assign them
                ResolveDependencies(unassignedMentors, unassignedMentees, assignedPeriods);
                //Console.WriteLine($"unassigned: {unassignedMentees.Count}");
                
                // if any mentor has now been assigned to a period, assign mentees to those periods where possible
                AssignMentees(unassignedMentees, assignedPeriods, menteesAssigned);
                //Console.WriteLine($"unassigned: {unassignedMentees.Count}");
                
                // assign a mentor to a period 
                if (!DependencyAwareAssignMentor(unassignedMentors, unassignedMentees, assignedPeriods))
                {
                    failedLoopCount++;
                }
                if (uCountPrev != unassignedMentees.Count)
                {
                    Console.WriteLine($"unassigned: {unassignedMentees.Count}");
                    uCountPrev = unassignedMentees.Count;
                }
            }
            if (unassignedMentees.Count == 0)
            {
                Console.WriteLine("Success");
            } else
            {
                Console.WriteLine("Failure");
                /*foreach(Person uMentee in unassignedMentees)
                {
                    Console.Write($"{uMentee} unassigned\nAvailable:");
                    foreach(var t in uMentee.Available)
                    {
                        Console.Write($"{t}  ");
                    }
                }*/
            }

            // verify the results
            Console.WriteLine("Verification " + (Verify(assignedPeriods, menteesAssigned) ? "successful" : "failed"));

            // assign the remainder of the mentees to those periods with the highest ratio of mentees to mentors
            List<Person>[,] mentorsAssigned = new List<Person>[5, 10];
            for(int d = 0; d < 5; d++)
            {
                for (int p = 0; p < 10; p++)
                {
                    mentorsAssigned[d, p] = new List<Person>();
                    mentorsAssigned[d, p].Add(assignedPeriods[d, p]);
                }
            }

            List<Person> assigned = new List<Person>();
            foreach(Person mentor in unassignedMentors)
            {
                double maxRatio = 0.0;
                Tuple<int, int> maxRatioPeriod = null;
                foreach(var t in mentor.Available)
                {
                    double currRatio = menteesAssigned[t.Item1, t.Item2].Count / (double)mentorsAssigned[t.Item1, t.Item2].Count;
                    if (currRatio > maxRatio)
                    {
                        maxRatio = currRatio;
                        maxRatioPeriod = t;
                    }
                }
                if (maxRatioPeriod != null) {
                    mentorsAssigned[maxRatioPeriod.Item1, maxRatioPeriod.Item2].Add(mentor);
                    assigned.Add(mentor);
                }
            }

            foreach(Person mentor in assigned)
            {
                unassignedMentors.Remove(mentor);
            }

            if (unassignedMentors.Count != 0)
            {
                Console.WriteLine($"{unassignedMentors.Count} mentors cannot be assigned");
            }

            Console.ReadLine();
        }

        static void ResolveDependencies(List<Person> unassignedMentors, List<Person> unassignedMentees, Person[,] assignedPeriods)
        {
            // the following loop will discover and resolve dependent mentees, updating the 
            // the unassignable periods list, until this is resolved
            int dependentMenteeCount = 0;
            do
            {
                bool[,] unassignablePeriods = UnassignablePeriods(unassignedMentors, assignedPeriods);

                //Showing unassignable periods
                /*Console.WriteLine("Unassignable periods:");
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        Console.Write(unassignablePeriods[i, j] ? "[X]" : "[ ]");
                    }
                    Console.WriteLine();
                }//*/

                dependentMenteeCount = 0;
                List<Person> assigned = new List<Person>();
                foreach (Person mentee in unassignedMentees)
                {
                    Tuple<Person, int, int> dependency = GetDependency(mentee, unassignedMentors, unassignablePeriods);
                    if (dependency.Item1 != null)
                    {
                        dependentMenteeCount++;
                        assignedPeriods[dependency.Item2, dependency.Item3] = dependency.Item1;
                        //Console.WriteLine($"Assigned {dependency.Item1}");
                        unassignedMentors.Remove(dependency.Item1);
                        assigned.Add(mentee);
                    }
                }
                foreach(Person p in assigned)
                {
                    unassignedMentees.Remove(p);
                }
            } while (dependentMenteeCount > 0);
        }

        /*
         * For periods that have a mentor assigned, assign mentees that are available in that period
         * For mentees available in two periods, a mentee will be assigned to the period with the fewest mentees assigned
         */
        static void AssignMentees(List<Person> unassignedMentees, Person[,] assignedPeriods, List<Person>[,] menteesAssigned)
        {
            List<Person> assigned = new List<Person>();
            foreach(Person mentee in unassignedMentees)
            {
                Tuple<int, int> tMin = null;
                int minCount = 1000;
                foreach(var t in mentee.Available)
                {
                    if (assignedPeriods[t.Item1, t.Item2] != null)
                    {
                        if (menteesAssigned[t.Item1, t.Item2].Count < minCount)
                        {
                            minCount = menteesAssigned[t.Item1, t.Item2].Count;
                            tMin = t;
                        }
                    }
                }
                if (tMin != null)
                {
                    menteesAssigned[tMin.Item1, tMin.Item2].Add(mentee);
                    assigned.Add(mentee);
                }
            }
            foreach(Person p in assigned)
            {
                unassignedMentees.Remove(p);
            }
        }

        static bool DependencyAwareAssignMentor(List<Person> unassignedMentors, List<Person> unassignedMentees, Person[,] assignedPeriods)
        {
            // this will assign the mentor that creates the fewest dependencies 
            List<Person> unassignedMentorCopy = null;
            Person[,] assignedPeriodsCopy = new Person[5,10];
            bool[,] unassignableTemp = null;

            Tuple<int, int> tMin = null;
            int minCount = 1000;
            Person minMentor = null;

            foreach (Person mentor in unassignedMentors)
            {
                unassignedMentorCopy = new List<Person>(unassignedMentors);
                unassignedMentorCopy.Remove(mentor);
                foreach (var t in mentor.Available)
                {
                    if (assignedPeriods[t.Item1, t.Item2] == null)
                    {
                        Array.Copy(assignedPeriods, assignedPeriodsCopy, assignedPeriodsCopy.Length);
                        assignedPeriodsCopy[t.Item1, t.Item2] = mentor;
                        int count = 0;
                        unassignableTemp = UnassignablePeriods(unassignedMentorCopy, assignedPeriodsCopy);
                        foreach (Person mentee in unassignedMentees)
                        {
                            if (GetDependency(mentee, unassignedMentorCopy, unassignableTemp).Item1 != null)
                            {
                                count++;
                            }
                        }
                        if (count < minCount)
                        {
                            minMentor = mentor;
                            tMin = t;
                        }
                    }
                }
            }
            if (tMin != null) // => mentorMax != null
            {
                assignedPeriods[tMin.Item1, tMin.Item2] = minMentor;
                unassignedMentors.Remove(minMentor);
                return true;
            }
            else
            {
                //Console.WriteLine("Unable to assign a mentor to a period");
                return false;
            }
        }

        /*
         * Assigns a mentor to the period with the most available mentees
         */
        static bool AssignMentor(List<Person> unassignedMentors, List<Person> unassignedMentees, Person[,] assignedPeriods)
        {
            Tuple<int, int> tMax = null;
            Person mentorMax = null;
            int maxCount = 0;
            foreach(Person mentor in unassignedMentors)
            {
                foreach(var t in mentor.Available)
                {
                    int count = 0;
                    if (assignedPeriods[t.Item1, t.Item2] == null)
                    {
                        foreach(Person mentee in unassignedMentees)
                        {
                            if (mentee.GetAvailable(t.Item1, t.Item1)) count++;
                        }
                    }
                    if (count > maxCount)
                    {
                        tMax = t;
                        mentorMax = mentor;
                        maxCount = count;
                    }
                }
            }

            if (tMax != null) // => mentorMax != null
            {
                assignedPeriods[tMax.Item1, tMax.Item2] = mentorMax;
                unassignedMentors.Remove(mentorMax);
                return true;
            } else
            {
                //Console.WriteLine("Unable to assign a mentor to a period");
                return false;
            }
        }

        /*
         * Periods to which it is never possible to assign a mentor (i.e., no mentor is available)
         * UnassignablePeriods[d,p] == true => the period (d,p) has no available mentor
         */
        static bool[,] UnassignablePeriods(List<Person> unassignedMentors, Person[,] assignedMentors)
        {
            bool[,] unassignable = new bool[5,10];
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 10; j++)
                    unassignable[i, j] = true;
            }

            foreach(Person mentor in unassignedMentors)
            {
                for (int d = 0; d < 5; d++)
                {
                    for (int p = 0; p < 10; p++)
                    {
                        if (unassignable[d,p] && assignedMentors[d,p] != null || mentor.GetAvailable(d,p))
                        {
                            unassignable[d, p] = false;
                        }
                    }
                }
            }

            return unassignable;
        }

        /*
         * Returns the triple (mentor, day, period) of a mentee if the mentee only has one possible assignable period;
         * otherwise returns (null, -1, -1)
         */
        static Tuple<Person, int, int> GetDependency(Person mentee, List<Person> unassignedMentors, bool[,] unassignablePeriods)
        {
            int count = 0, dayC = -1, perC = -1;
            Person mentorC = null;

            foreach (var t in mentee.Available)
            {
                if (!unassignablePeriods[t.Item1, t.Item2])
                {
                    if (count > 0)
                    {
                        // the mentee has more than one assignable period
                        return new Tuple<Person, int, int>(null, -1, -1);
                    }
                    count++;
                    dayC = t.Item1;
                    perC = t.Item2;
                    foreach (Person mentor in unassignedMentors)
                    {
                        if (mentor.GetAvailable(dayC, perC)) mentorC = mentor;
                    }
                }
            }
            return new Tuple<Person, int, int>(mentorC, dayC, perC);
        }

        /*
         * generate a list of Person objects with randomized schedules for simulation
         */
        static List<Person> GeneratePeople(int count, Role role, int probabilityOfBusiness)
        {
            List<Person> generated = new List<Person>();
            Person curr = null;
            Random r = new Random();
            for (int i = 0; i < count; i++)
            {
                curr = new Person(role);
                for (int day = 0; day < 5; day++)
                {
                    for (int per = 0; per < 10; per++)
                    {
                        curr.SetAvailable(day, per, r.Next(1, 101) > probabilityOfBusiness); // p% probability of being busy
                    }
                }
                generated.Add(curr);
            }
            return generated;
        }

        static bool Verify(Person[,] assignedPeriods, List<Person>[,] menteesAssigned)
        {
            // verification
            List<Person> mentorVerify = new List<Person>();
            List<Person> menteeVerify = new List<Person>();
            bool valid = true;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (assignedPeriods[i, j] != null)
                    {
                        if (mentorVerify.Contains(assignedPeriods[i, j]))
                        {
                            Console.WriteLine($"{assignedPeriods[i, j]} duplicate assignment");
                            valid = false;
                        }
                        else
                        {
                            mentorVerify.Add(assignedPeriods[i, j]);
                            if (!assignedPeriods[i, j].GetAvailable(i, j))
                            {
                                Console.WriteLine($"{assignedPeriods[i, j]} assigned to ({i},{j}) but not available");
                                valid = false;
                            }
                        }
                    }
                    foreach (Person mentee in menteesAssigned[i, j])
                    {
                        if (menteeVerify.Contains(mentee))
                        {
                            Console.WriteLine($"{mentee} duplicate assignment");
                            valid = false;
                        }
                        else
                        {
                            menteeVerify.Add(mentee);
                            if (!mentee.GetAvailable(i, j))
                            {
                                Console.WriteLine($"{mentee} assigned to ({i},{j}) but not available");
                                valid = false;
                            }
                        }
                    }
                }

            }

            return valid;
        }
    }

    class Person
    {
        Role role;
        static int nextId = 0;
        private int id;
        private HashSet<Tuple<int, int>> available;
        private Tuple<int, int> selected;
        public Person(Role role)
        {
            available = new HashSet<Tuple<int, int>>();
            id = nextId++;
            this.role = role;
        }

        public void SetAvailable(int day, int period, bool avail)
        {
            var t = new Tuple<int, int>(day, period);

            // HashSets only store unique elements 
            // so no need to check for existing t = (day, period)
            if (avail)
            {
                available.Add(t);
                if (selected == null) selected = t;
            } else if (available.Contains(t))
            {
                available.Remove(t);
            }
        }

        public bool GetAvailable(int day, int period)
        {
            return available.Contains(new Tuple<int, int>(day, period));
        }

        public int ID
        {
            get { return id; }
        }

        public int AvailableCount
        {
            get
            {
                return available.Count;
            }
        }

        public Tuple<int, int> Selected
        {
            get { return selected; }
            set
            {
                if (available.Contains(value))
                {
                    selected = value;
                }
            }
        }

        public HashSet<Tuple<int, int>> Available
        {
            get
            {
                return available;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Person)) return false;
            return ((Person)obj).id == this.id;
        }

        public override string ToString()
        {
            return $"({id}, {role})";
        }
    }

    enum Role
    {
        Mentor, 
        Mentee
    }
}
