using System;
using System.Linq;
using calculator_demo.data;
using eventorientation;
using Version = eventorientation.Version;

namespace calculator_demo.pipelines
{
    public class AppendOperatorPipeline : ICommandPipeline<AppendOperator,AppendOperatorPipeline.Model>
    {
        public class Model : MessageModel {
            public int Number;
            public int Result;
            public AppendOperator.Operators PreviousOperator;
            public bool NumberUpdatedAfterOp;
            
            public static Model Neutral => new Model{Number = 0, Result = 0, PreviousOperator = AppendOperator.Operators.Addition};
        }



        public (Model model, Version version) Load(IEventstore es, AppendOperator command) {
            var model = Model.Neutral;
            foreach (var e in es.Replay()) {
                switch (e) {
                    case NumberUpdated nu:
                        model.Number = nu.Number;
                        model.NumberUpdatedAfterOp = true;
                        break;
                    case ResultCalculated rc:
                        model.Result = rc.Number;
                        break;
                    case OpAppended oa:
                        model.PreviousOperator = oa.ToEnum();
                        model.NumberUpdatedAfterOp = false;
                        break;
                }                
            }
            return (model, null);
        }

        
        public (CommandStatus commandStatus, Event[] events, Notification[] notifications) Execute(AppendOperator command, Model model) {
            model = Adjust_for_previous_Equals(model);
            
            if (Op_replacement_requested(model))
                return (
                        new Success(),
                        new[] {
                            command.Op.ToEvent()
                        },
                        new Notification[0]
                );

            try {
                return (
                    new Success(),
                    new[] {
                        new ResultCalculated {Number = Eval(model.Result, model.PreviousOperator, model.Number)},
                        new NumberUpdated {Number = 0},
                        command.Op.ToEvent()
                    },
                    new Notification[0]
                );
            }
            catch (Exception e) {
                return (
                    new Failure(e.Message),
                    new Event[0],
                    new Notification[0]
                );
            }


            static Model Adjust_for_previous_Equals(Model model) {
                if (model.PreviousOperator == AppendOperator.Operators.Equals && model.NumberUpdatedAfterOp)
                    model.Result = model.Number;
                return model;
            }

            static bool Op_replacement_requested(Model model)
                => model.PreviousOperator != AppendOperator.Operators.Equals &&
                   model.NumberUpdatedAfterOp is false;
        }


        public void Update(IEventstore es, Event[] events, Version version) {}



        private static int Eval(int a, AppendOperator.Operators op, int b) {
            return op switch {
                AppendOperator.Operators.Addition => a + b,
                AppendOperator.Operators.Subtraction => a - b,
                AppendOperator.Operators.Multiplication => Divide(a, b),
                AppendOperator.Operators.Division => a / b,
                AppendOperator.Operators.Equals => a,
            };


            static int Divide(int a, int b) {
                if (b == 0) throw new DivideByZeroException();
                return a / b;
            }
        }
    }


    static class OpEventExtensions
    {
        public static Event ToEvent(this AppendOperator.Operators op)
            => op switch {
                AppendOperator.Operators.Addition => (Event)new AdditionAppended(),
                AppendOperator.Operators.Subtraction => new SubtractionAppended(),
                AppendOperator.Operators.Multiplication => new MultiplicationAppended(),
                AppendOperator.Operators.Division => new DivisionAppended(),
                AppendOperator.Operators.Equals => new EqualsAppended()
            };


        public static AppendOperator.Operators ToEnum(this OpAppended e)
        {
            return e switch {
                AdditionAppended _ => AppendOperator.Operators.Addition,
                SubtractionAppended _ => AppendOperator.Operators.Subtraction,
                MultiplicationAppended _ => AppendOperator.Operators.Multiplication,
                DivisionAppended _ => AppendOperator.Operators.Division,
                EqualsAppended _ => AppendOperator.Operators.Equals
            };
        }
    }
}