using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace TrelloMicroService
{
    using CommonMessages;
    using EasyNetQ;
    using EasyNetQ.MessageVersioning;
    using EasyNetQ.Topology;
    using ExpectedObjects;
    using JetBrains.Annotations;
    using MicroServiceEcoSystem;
    using NodaTime;
    using Topshelf;
    using TrelloNet;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A trello micro service. </summary>
    ///
    /// <seealso cref="T:MicroServiceEcoSystem.BaseMicroService{TrelloMicroService.TrelloMicroService, CommonMessages.TrelloResponseMessage}"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class TrelloMicroService : BaseMicroService<TrelloMicroService, TrelloResponseMessage>
    {
        //TrelloAuthorization.Default.AppKey = "9dbf8c09499d07abac02bbd6d5af4b9c";
        //TrelloAuthorization.Default.UserToken = "95da70bf03bd43b82648f515477d44ec84baa2fb9e811cb7284be10d94512b81";

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the trello. </summary>
        ///
        /// <value> The trello. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public ITrello trello { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the board. </summary>
        ///
        /// <value> The board. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Board board { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the list. </summary>
        ///
        /// <value> The list. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public List list { get; set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the card. </summary>
        ///
        /// <value> The card. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public Card card { get; set; }
/// <summary>   The random. </summary>
private Random random = new Random();

private const string TrelloApiBaseUrl = "https://api.trello.com/1";
private const string TrelloApiKey = "b4596a2069491cce445777712c528041";
private const string TrelloToken = "ATTA44fbd9ca6fb4c658954ee0295d1ab554160322c217aaa667e5b16cf10276f1fcA4CBB72D";

private string boardId;
private string listId;
private string cardId;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initializes a new instance of the TrelloMicroService.TrelloMicroService class.
        /// </summary>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public TrelloMicroService()
        {
            Name = "Trello Microservice_" + Environment.MachineName;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the start action. </summary>
        ///
        /// <param name="host"> The host. This may be null. </param>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStart([CanBeNull] HostControl host)
        {
            base.Start(host);
            Subscribe();
            trello = new Trello("b4596a2069491cce445777712c528041"); 
            var url = trello.GetAuthorizationUrl("dummy", Scope.ReadWrite);
            trello.Authorize("ATTA44fbd9ca6fb4c658954ee0295d1ab554160322c217aaa667e5b16cf10276f1fcA4CBB72D");




            var expectedBoard = CreateBoard("Microservice Ecosystem", "Hands on Microservices with C#");

            // SAFEGUARD: With old TrelloNet, the board object may be null even when Trello created the board.
            // Therefore check the id returned by the direct REST call instead.
            if (string.IsNullOrEmpty(boardId))
            {
                Console.WriteLine("CRITICAL ERROR: Trello API failed to create the board or did not return a board id.");
                return false;
            }

            var expectedList = CreateList("Drafts");

            for (int x=0; x<10; x++)
                CreateCard("Chapter " + (x + 1).ToString(), RandomString(25), false,
                    x % 2 == 0
                        ? SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(12)
                        : DateTime.MinValue);

            CreateList("Proofs");

            for (int x = 0; x < 10; x++)
                CreateCard("Chapter " + (x + 1).ToString(), RandomString(25), false,
                    x % 2 == 0
                        ? SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(24)
                        : DateTime.MinValue);

            CreateList("Final Copies");

            for (int x = 0; x < 10; x++)
                CreateCard("Chapter " + (x + 1).ToString(), RandomString(25), false,
                    x % 2 == 0
                        ? SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc().ToLocalTime().AddDays(36)
                        : DateTime.MinValue);

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the stop action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnStop()
        {
            base.Stop();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the continue action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnContinue()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the pause action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnPause()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the resume action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnResume()
        {
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Executes the shutdown action. </summary>
        ///
        /// <returns>   True if it succeeds, false if it fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public new bool OnShutdown()
        {
            return true;
        }

        /// <summary>   Subscribes this object. </summary>
        public void Subscribe()
        {
            Bus = RabbitHutch.CreateBus("host=localhost",
                x =>
                {
                    x.Register<IConventions, AttributeBasedConventions>();
                    x.EnableMessageVersioning();
                });

            IExchange exchange = Bus.Advanced.ExchangeDeclare("EvolvedAI", ExchangeType.Topic);
            IQueue queue = Bus.Advanced.QueueDeclare("MachineLearning");
            Bus.Advanced.Bind(exchange, queue, "");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a board. </summary>
        ///
        /// <param name="name"> The name. </param>
        /// <param name="desc"> The description. </param>
        ///
        /// <returns>   The new board. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

private ExpectedObject CreateBoard(string name, string desc)
{
    var client = new RestClient(TrelloApiBaseUrl);
    var request = new RestRequest("boards", Method.POST);

    request.AddParameter("key", TrelloApiKey);
    request.AddParameter("token", TrelloToken);
    request.AddParameter("name", name);
    request.AddParameter("desc", desc);
    request.AddParameter("defaultLists", "false");
    request.AddParameter("prefs_permissionLevel", "public");
    request.AddParameter("prefs_voting", "members");
    request.AddParameter("prefs_comments", "members");
    request.AddParameter("prefs_invitations", "members");

    var response = client.Execute(request);

    if (response == null || response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.MultipleChoices)
    {
        Console.WriteLine("CreateBoard failed: " + (response == null ? "No response" : response.Content));
        boardId = null;
        return null;
    }

    var json = JObject.Parse(response.Content);
    boardId = json.Value<string>("id");

    if (string.IsNullOrEmpty(boardId))
    {
        Console.WriteLine("CreateBoard failed: Trello did not return a board id. Response: " + response.Content);
        return null;
    }

    Console.WriteLine("Created Trello board: " + name + " / id: " + boardId);

    return new
    {
        Id = boardId,
        Name = name,
        Desc = desc
    }.ToExpectedObject();
}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a list. </summary>
        ///
        /// <param name="name"> The name. </param>
        ///
        /// <returns>   The new list. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

private ExpectedObject CreateList(string name)
{
    if (string.IsNullOrEmpty(boardId))
    {
        Console.WriteLine("CreateList failed: boardId is empty.");
        return null;
    }

    var client = new RestClient(TrelloApiBaseUrl);
    var request = new RestRequest("lists", Method.POST);

    request.AddParameter("key", TrelloApiKey);
    request.AddParameter("token", TrelloToken);
    request.AddParameter("name", name);
    request.AddParameter("idBoard", boardId);
    request.AddParameter("pos", "bottom");

    var response = client.Execute(request);

    if (response == null || response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.MultipleChoices)
    {
        Console.WriteLine("CreateList failed: " + (response == null ? "No response" : response.Content));
        listId = null;
        return null;
    }

    var json = JObject.Parse(response.Content);
    listId = json.Value<string>("id");

    if (string.IsNullOrEmpty(listId))
    {
        Console.WriteLine("CreateList failed: Trello did not return a list id. Response: " + response.Content);
        return null;
    }

    Console.WriteLine("Created Trello list: " + name + " / id: " + listId);

    return new
    {
        Id = listId,
        Name = name,
        IdBoard = boardId
    }.ToExpectedObject();
}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates a card. </summary>
        ///
        /// <param name="name">     The name. </param>
        /// <param name="desc">     The description. </param>
        /// <param name="closed">   True if closed. </param>
        /// <param name="dueDate">  The due date. </param>
        ///
        /// <returns>   The new card. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

private ExpectedObject CreateCard(string name, string desc, bool closed, DateTime dueDate)
{
    if (string.IsNullOrEmpty(listId))
    {
        Console.WriteLine("CreateCard failed: listId is empty.");
        return null;
    }

    var client = new RestClient(TrelloApiBaseUrl);
    var request = new RestRequest("cards", Method.POST);

    request.AddParameter("key", TrelloApiKey);
    request.AddParameter("token", TrelloToken);
    request.AddParameter("idList", listId);
    request.AddParameter("name", name);
    request.AddParameter("desc", desc);
    request.AddParameter("closed", closed.ToString().ToLowerInvariant());

    if (dueDate > DateTime.MinValue)
        request.AddParameter("due", dueDate.ToString("o"));

    var response = client.Execute(request);

    if (response == null || response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.MultipleChoices)
    {
        Console.WriteLine("CreateCard failed: " + (response == null ? "No response" : response.Content));
        cardId = null;
        return null;
    }

    var json = JObject.Parse(response.Content);
    cardId = json.Value<string>("id");

    if (string.IsNullOrEmpty(cardId))
    {
        Console.WriteLine("CreateCard failed: Trello did not return a card id. Response: " + response.Content);
        return null;
    }

    Console.WriteLine("Created Trello card: " + name + " / id: " + cardId);

    if (dueDate == DateTime.MinValue)
    {
        AddLabelToCard(cardId, "green");
    }

    AddCommentToCard(cardId, RandomString(50));

    return new
    {
        Id = cardId,
        Name = name,
        Desc = desc,
        IdList = listId
    }.ToExpectedObject();
}

private void AddLabelToCard(string trelloCardId, string color)
{
    var client = new RestClient(TrelloApiBaseUrl);
    var request = new RestRequest("cards/{id}/labels", Method.POST);

    request.AddUrlSegment("id", trelloCardId);
    request.AddParameter("key", TrelloApiKey);
    request.AddParameter("token", TrelloToken);
    request.AddParameter("color", color);

    var response = client.Execute(request);

    if (response == null || response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.MultipleChoices)
    {
        Console.WriteLine("AddLabelToCard failed: " + (response == null ? "No response" : response.Content));
    }
}

private void AddCommentToCard(string trelloCardId, string text)
{
    var client = new RestClient(TrelloApiBaseUrl);
    var request = new RestRequest("cards/{id}/actions/comments", Method.POST);

    request.AddUrlSegment("id", trelloCardId);
    request.AddParameter("key", TrelloApiKey);
    request.AddParameter("token", TrelloToken);
    request.AddParameter("text", text);

    var response = client.Execute(request);

    //response.StatusCode < HttpStatusCode.OK || response.StatusCode >= HttpStatusCode.MultipleChoices>
    
    {
        Console.WriteLine("AddCommentToCard failed: " + (response == null ? "No response" : response.Content));
    }
}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Creates check list. </summary>
        ///
        /// <param name="name"> The name. </param>
        ///
        /// <returns>   The new check list. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private ExpectedObject CreateCheckList(string name)
        {
            BoardId bi = new BoardId(board.GetBoardId());
            var aNewChecklist = trello.Checklists.Add(name, board);


            //ChecklistId id = new ChecklistId(aNewChecklist.GetChecklistId());
            //trello.Cards.AddChecklist(card, id);

            // Add check items
            trello.Checklists.AddCheckItem(aNewChecklist, "First Draft");
            return aNewChecklist.ToExpectedObject();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Random string. </summary>
        ///
        /// <param name="length">   The length. </param>
        ///
        /// <returns>   A string. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
