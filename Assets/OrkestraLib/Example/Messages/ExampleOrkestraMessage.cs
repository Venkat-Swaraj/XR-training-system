using System;

namespace OrkestraLib
{
    namespace Message
    {
        [Serializable]
        public class ExampleOrkestraMessage : Message
        {
           
            public string message;

            public ExampleOrkestraMessage(string json) : base(json) { }


            public ExampleOrkestraMessage(string userId, string message) :
                base(typeof(ExampleOrkestraMessage).Name, userId)
            {
                this.message = message;
            }


            public override string FriendlyName()
            {
                return typeof(ExampleOrkestraMessage).Name;
            }
        }
    }
}
