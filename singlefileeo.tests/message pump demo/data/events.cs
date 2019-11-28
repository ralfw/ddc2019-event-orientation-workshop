namespace eventorientation.tests.message_pump_demo.data
{
    class Incremented : Event
    {
        public string CounterId;
    }

    class Decremented : Event
    {
        public string CounterId;
    }
}