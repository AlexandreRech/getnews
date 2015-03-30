using System;
using System.Collections.Generic;

namespace GetNewsFromUniplac.MinimalCometLibrary
{
    public delegate void QueryHandler(Query query, Dictionary<string, object> data);

    public class Query
    {
        private readonly QueryHandler queryCallback;        

        private int jobsCompleted;
        private int jobsStarted;
        private int jobsSpawned;

        public Query(QueryHandler queryCallback)
        {
            this.queryCallback = queryCallback;
        }

        public bool IsFinished { get; private set; }

        public void OnMessage(Dictionary<string, object> data)
        {
            var messageType = (string)data["type"];

            Console.WriteLine((string)data["type"]);

            switch (messageType)
            {
                case "SPAWN":
                    jobsSpawned++;
                    break;
                case "INIT":
                case "START":
                    jobsStarted++;
                    break;
                case "STOP":
                    jobsCompleted++;
                    break;
            }

            IsFinished = jobsStarted == jobsCompleted && jobsSpawned + 1 == jobsStarted && jobsStarted > 0;

            if (messageType.Equals("ERROR") || messageType.Equals("UNAUTH") || messageType.Equals("CANCEL"))
            {
                IsFinished = true;
            }

            queryCallback(this, data);            
        }
    }

   
}

