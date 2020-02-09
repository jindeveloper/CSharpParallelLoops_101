using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CSharpParallelLoops_101
{
    public class Parallel_Loops_UnitTest
    {
        private readonly ITestOutputHelper _output;
        public Parallel_Loops_UnitTest(ITestOutputHelper output)
        {
            this._output = output;
        }

        /// <summary>
        /// In this example we are going to download the an html page from Microsoft page having this url 'https://www.microsoft.com/en-ph/'.
        /// We are going to count the number of span, paragraph and links in the page in parallel.
        /// When you debug this sample unit test you'll see that it behaves differently. 
        /// Meaning it can run in order based on the delegate actions that we passed. In our case we only passed 3 Action delegates.
        /// </summary>
        [Fact]
        public void Test_Parallel_Invoke_Passing_An_Action_Delegate()
        {
            //Define the values needed when the parallel.invoke ended and joined with the main thread.
            int totalSpan = 0;
            int totalParagraph = 0;
            int totalLink = 0;

            //define the URL needed to count the following elements: span, paragraph, and link. 
            Uri url = new Uri("https://www.microsoft.com/en-ph/");

            Parallel.Invoke(
                () =>
                    {
                        this._output.WriteLine("started a task for counting the span elements");

                        var client = new WebClient();

                        var text = client.DownloadString(url.AbsoluteUri);

                        var doc = new HtmlDocument();

                        doc.LoadHtml(text);

                        totalSpan = doc.DocumentNode.SelectNodes("//span").Count();
                    },
                () =>
                    {
                        this._output.WriteLine("started a task for counting the paragraph elements");

                        var client = new WebClient();

                        var text = client.DownloadString(url.AbsoluteUri);

                        var doc = new HtmlDocument();

                        doc.LoadHtml(text);

                        totalParagraph = doc.DocumentNode.SelectNodes("//p").Count();
                    },
                () =>
                    {
                        this._output.WriteLine("started a task for counting the link elements");

                        var client = new WebClient();

                        var text = client.DownloadString(url.AbsoluteUri);

                        var doc = new HtmlDocument();

                        doc.LoadHtml(text);

                        totalLink = doc.DocumentNode.SelectNodes("//link").Count();
                    });

            Assert.True(totalLink > 0);
            Assert.True(totalParagraph > 0);
            Assert.True(totalSpan > 0);
        }

        /// <summary>
        /// In this example we are going to throw and handle an exception from the Action delegates
        /// </summary>
        [Fact]
        public void Test_Parallel_Invoke_Passing_An_Action_And_HandleException()
        {
            //Define the values needed when the parallel.invoke ended and joined with the main thread.

            int totalSpan = 0;
            int totalParagraph = 0;
            int totalLink = 0;

            //define the url needed to count the following elements: span, paragraph and link.
            Uri url = new Uri("https://www.microsoft.com/en-ph/");

            /*We no longer finish the method to its proper functionality.
            * We just need to throw an exception and see how it can be handled.
            */
            Func<string, string, Uri, int> getTotalElement = delegate (string message, string element, Uri url)
            {
                throw new Exception($"Random exception from {element}");
            };

            #region prove-that-parallel-invoke-throws-an-aggregate-exception
            var exception = Assert.Throws<AggregateException>(() => {

                Parallel.Invoke(
                   () => totalSpan = getTotalElement("started to get the span element", "//span", url),
                   () => totalParagraph = getTotalElement("started to get the paragraph element", "//p", url),
                   () => totalLink = getTotalElement("started to get and count total link element", "//link", url));

            });

            //let's assert if all messages are equal to 'Random exception'
            Assert.True(((AggregateException)exception).Flatten().InnerExceptions.All(x => x.Message.Contains("Random exception")));

            #endregion

            //in this section will try to handle the exception via the try-catch code block
            try
            {
                Parallel.Invoke(
                () => totalSpan = getTotalElement("started to get the span element", "//span", url),
                () => totalParagraph = getTotalElement("started to get the paragraph element", "//p", url),
                () => totalLink = getTotalElement("started to get and count total link element", "//link", url));
            }
            catch (AggregateException aggregateException)
            {
                foreach (Exception error in aggregateException.Flatten().InnerExceptions)
                {
                    this._output.WriteLine(error.Message);
                }
            }
            //end of try-catch code block
        }

        /// <summary>
        /// In this example we are going to download the an html page from Microsoft page having this url 'https://www.microsoft.com/en-ph/'.
        /// We are going to count the number of span, paragraph and links in the page in parallel.
        /// When you debug this sample unit test you'll see that it behaves differently. 
        /// Meaning it can run in order based on the delegate actions that we passed. In our case we only passed 3 Action delegates.
        /// However; will be cancelling the functionality. 
        /// </summary>
        [Fact]
        public void Test_Parallel_Invoke_Passing_An_Action_Delegate_And_CancellationToken()
        {
            //Define the values needed when the parallel.invoke ended and joined with the main thread.
            int totalSpan = 0;
            int totalParagraph = 0;
            int totalLink = 0;

            //define the URL needed to count the following elements: span, paragraph, and link.
            Uri url = new Uri("https://www.microsoft.com/en-ph/");

            //Define the cancellation token source we need to cancel even before the parallel.invoke is executed
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            //Define the ParallelOptions class to be passed as an argument to the first parameter of Parallel.Invoke
            ParallelOptions options = new ParallelOptions { CancellationToken = token };

            Func<CancellationToken, string, string, Uri, int> getTotalElement = delegate (CancellationToken token, string message, string element, Uri url)
            {
                this._output.WriteLine(message);

                token.ThrowIfCancellationRequested();

                var client = new WebClient();

                var text = client.DownloadString(url.AbsoluteUri);

                var doc = new HtmlDocument();

                doc.LoadHtml(text);

                return doc.DocumentNode.SelectNodes(element).Count();
            };

            //you can uncomment this to see the full action
            tokenSource.Cancel(); //lets cancel

            try
            {
                Parallel.Invoke(options,
                () => totalSpan = getTotalElement(token, "started to get the span element", "//span", url),
                () => totalParagraph = getTotalElement(token, "started to get the paragraph element", "//p", url),
                () => totalLink = getTotalElement(token, "started to get and count total link element", "//link", url));
            }
            catch (OperationCanceledException operationCanceled)
            {
                this._output.WriteLine(operationCanceled.Message);
            }


            Assert.True(totalLink == 0);
            Assert.True(totalParagraph == 0);
            Assert.True(totalSpan == 0);

        }


        /// <summary>  
        /// In this example we are going to download the HTML page from Microsoft page having this URL 'https://www.microsoft.com/en-ph/'.  
        /// We are going to count the number of the span, paragraph, and links in the page in parallel.  
        /// When you debug this sample unit test you'll see that it behaves differently.   
        /// Meaning it can run in order based on the delegate actions that we passed. In our case, we only passed 4 Action delegates.  
        /// However; we only need to invoke two methods at a time.   
        /// </summary> 
        [Fact]
        public void Test_Parallel_Invoke_Passing_An_Action_Delegate_And_MaxDegreeOfParallelism()
        {
            //Define the values needed when the parallel.invoke ended and joined with the main thread.
            int totalSpan = 0;
            int totalParagraph = 0;
            int totalLink = 0;
            int totalDiv = 0;

            //define the url needed to count the following elements: span, paragraph and link.
            Uri url = new Uri("https://www.microsoft.com/en-ph/");

            //Define the ParallelOptions class to be passed as an argument to the first parameter of Parallel.Invoke. 
            //Then set the MaxDegreeOfParllelism to two (2)
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = 2 };

            Func<string, string, Uri, int> getTotalElement = delegate (string message, string element, Uri url)
            {
                this._output.WriteLine($"{message} and Task Id: {Task.CurrentId}");

                var client = new WebClient();

                var text = client.DownloadString(url.AbsoluteUri);

                var doc = new HtmlDocument();

                doc.LoadHtml(text);

                return doc.DocumentNode.SelectNodes(element).Count();
            };

            try
            {
                Parallel.Invoke(options,
                () => totalSpan = getTotalElement("started to get the span element", "//span", url),
                () => totalParagraph = getTotalElement("started to get the paragraph element", "//p", url),
                () => totalLink = getTotalElement("started to get and count total link element", "//link", url),
                () => totalDiv = getTotalElement("started to get and count div element", "//div", url));
            }
            catch (OperationCanceledException operationCanceled)
            {
                this._output.WriteLine(operationCanceled.Message);
            }
            catch(AggregateException aggregateException)
            {
                foreach (Exception error in aggregateException.Flatten().InnerExceptions)
                {
                    this._output.WriteLine(error.Message);
                }
            }

            Assert.True(totalLink > 0);
            Assert.True(totalParagraph > 0);
            Assert.True(totalSpan > 0);
            Assert.True(totalDiv > 0);
        }

        /// <summary>
        /// Let's just iterate 0 to 30 using the Parallel.For
        /// </summary>
        [Fact]
        public void Test_Parallel_For()
        {
            this._output.WriteLine("--------Start--------");
            this._output.WriteLine("Warning: Not in order");

            Parallel.For(0, 30, counter => { this._output.WriteLine($"Counter = {counter}, Task Id: {Task.CurrentId}"); });

            this._output.WriteLine("--------End--------");
        }

        /// <summary>
        /// Let's just iterate 0 to 30 using the Parallel.For with nested Parallel.For loop
        /// </summary>
        [Fact]
        public void Test_Parallel_Nested_For()
        {
            this._output.WriteLine("--------Start--------");
            this._output.WriteLine("Warning: Not in order");

            Parallel.For(0, 10, counter => 
                                { 
                                    this._output.WriteLine($"Counter = {counter}, Task Id: {Task.CurrentId}");
                                    
                                    Parallel.For(0, counter, innerCounter => {
                                        this._output.WriteLine($"Counter = {counter} at inner-counter-loop = {innerCounter}, Task Id: {Task.CurrentId}");
                                    });
                                });

            this._output.WriteLine("--------End--------");
        }

        /// <summary>
        /// Let's just iterate 0 to 100 by using Parallel.ForEach
        /// </summary>
        [Fact]
        public void Test_Parallel_ForEach()
        {
            IEnumerable<int> range = Enumerable.Range(0, 100);

            Parallel.ForEach(range, counter => {

                this._output.WriteLine($"{counter}");
            
            });
        }

        /// <summary>
        /// Let's just iterate 0 to 100 by using Parallel.ForEach
        /// </summary>
        [Fact]
        public void Test_Parallel_Nested_ForEach()
        {
            IEnumerable<int> range = Enumerable.Range(0, 100);

            Parallel.ForEach(range, counter => {

                this._output.WriteLine($"{counter}");

            });
        }

        /// <summary>
        /// Let's iterate through a list of customers and break
        /// </summary>
        [Fact]
        public void Test_Parallel_ForEach_Parallel_LoopState_Break()
        {
            var customers = new List<dynamic> 
            { 
                new { FirstName="Mark",    LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-18).Year) },
                new { FirstName="Anthony", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-14).Year) },
                new { FirstName="Jin",     LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-13).Year) },
                new { FirstName="Vincent", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-20).Year) },
            };

            ParallelLoopResult result = Parallel.ForEach(customers, (customer, loopState) => {
                
                this._output.WriteLine($"{customer.LastName}, {customer.FirstName} is below 18. Current age {customer.Age}");

                if (loopState.IsStopped) return;

                if (customer.Age < 18)
                {
                    this._output.WriteLine($"Breaking at the loop");

                    loopState.Break();
                }
            });

            Assert.True(!result.IsCompleted);
            Assert.True(result.LowestBreakIteration == 1);
            Assert.True(result.LowestBreakIteration != 0);
        }


        /// <summary>
        /// Let's iterate through a list of customers and stop
        /// </summary>
        [Fact]
        public void Test_Parallel_ForEach_Parallel_LoopState_Stop()
        {
            var customers = new List<dynamic>
            {
                new { FirstName="Mark",    LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-18).Year) },
                new { FirstName="Anthony", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-14).Year) },
                new { FirstName="Jin",     LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-13).Year) },
                new { FirstName="Vincent", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-20).Year) },
            };

            ParallelLoopResult result = Parallel.ForEach(customers, (customer, loopState) => {

                this._output.WriteLine($"{customer.LastName}, {customer.FirstName} is below 18. Current age {customer.Age}");

                if (loopState.IsStopped) return;

                if (customer.Age < 18)
                {
                    this._output.WriteLine($"Breaking at the loop");

                    loopState.Stop();
                }
            });

            Assert.True(!result.IsCompleted);
            Assert.True(!result.LowestBreakIteration.HasValue);
        }

        /// <summary>
        /// Let's iterate through a list of customers and break
        /// </summary>
        [Fact]
        public void Test_Parallel_For_Parallel_LoopState_Break()
        {
            var customers = new List<dynamic>
            {
                new { FirstName="Vincent", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-20).Year) },
                new { FirstName="Mark",    LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-18).Year) },
                new { FirstName="Anthony", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-14).Year) },
                new { FirstName="Jin",     LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-13).Year) }
                
            };

            var customerRange = Enumerable.Range(0, customers.Count);

            ParallelLoopResult result = Parallel.For(0, customers.Count,  (customerIndex, loopState) => {

                this._output.WriteLine($"{customers[customerIndex].LastName}, {customers[customerIndex].FirstName} is below 18. Current age {customers[customerIndex].Age}");

                if (loopState.IsStopped) return;

                if (customers[customerIndex].Age < 18)
                {
                    this._output.WriteLine($"Breaking at the loop");

                    loopState.Break();
                }
            });

            Assert.True(!result.IsCompleted);
            Assert.True(result.LowestBreakIteration == 2);
            Assert.True(result.LowestBreakIteration != 0);
        }

        /// <summary>
        /// Let's iterate through a list of customers and stop
        /// </summary>
        [Fact]
        public void Test_Parallel_For_Parallel_LoopState_Stop()
        {
            var customers = new List<dynamic>
            {
                new { FirstName="Vincent", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-20).Year) },
                new { FirstName="Mark",    LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-18).Year) },
                new { FirstName="Anthony", LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-14).Year) },
                new { FirstName="Jin",     LastName ="Necesario", Birthdate = DateTime.Now, Age =  (DateTime.Now.Year - DateTime.Now.AddYears(-13).Year) }

            };

            var customerRange = Enumerable.Range(0, customers.Count);

            ParallelLoopResult result = Parallel.For(0, customers.Count, (customerIndex, loopState) => {

                this._output.WriteLine($"{customers[customerIndex].LastName}, {customers[customerIndex].FirstName} is below 18. Current age {customers[customerIndex].Age}");

                if (loopState.IsStopped) return;

                if (customers[customerIndex].Age < 18)
                {
                    this._output.WriteLine($"Breaking at the loop");

                    loopState.Stop();
                }
            });

            Assert.True(!result.IsCompleted);
            Assert.True(!result.LowestBreakIteration.HasValue);

        }

    }
}
