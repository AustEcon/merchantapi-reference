﻿// Copyright (c) 2020 Bitcoin Association

using MerchantAPI.APIGateway.Rest.ViewModels;
using MerchantAPI.APIGateway.Test.Functional.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MerchantAPI.APIGateway.Test.Functional
{
  [TestClass]
  public class NodeRest : TestRestBase<NodeViewModelGet, NodeViewModelCreate>
  {
    [TestInitialize]
    public void TestInitialize()
    {
      Initialize(mockedServices: true);
      ApiKeyAuthentication = AppSettings.RestAdminAPIKey;
    }

    [TestCleanup]
    public void TestCleanup()
    {
      Cleanup();
    }

    public override string GetNonExistentKey() => "ThisKeyDoesNotExists:123";
    public override string GetBaseUrl() => MapiServer.ApiNodeUrl;
    public override string ExtractGetKey(NodeViewModelGet entry) => entry.Id;
    public override string ExtractPostKey(NodeViewModelCreate entry) => entry.Id;

    public override void SetPostKey(NodeViewModelCreate entry, string key)
    {
      entry.Id = key;
    }

    public override NodeViewModelCreate GetItemToCreate()
    {
      return new NodeViewModelCreate
      {
        Id = "some.host:123",
        Remarks = "Some remarks",
        Password = "somePassword",
        Username = "someUsername"
      };
    }

    public override void ModifyEntry(NodeViewModelCreate entry)
    {
      entry.Remarks += "Updated remarks";
      entry.Username += "updatedUsername";
    }

    public override NodeViewModelCreate[] GetItemsToCreate()
    {
      return
        new[]
        {
          new NodeViewModelCreate
          {
            Id = "some.host1:123",
            Remarks = "Some remarks1",
            Password = "somePassword1",
            Username = "user1"
          },

          new NodeViewModelCreate
          {
            Id = "some.host2:123",
            Remarks = "Some remarks2",
            Password = "somePassword2",
            Username = "user2"
          },
        };

    }

    public override void CheckWasCreatedFrom(NodeViewModelCreate post, NodeViewModelGet get)
    {
      Assert.AreEqual(post.Id.ToLower(), get.Id.ToLower()); // Ignore key case
      Assert.AreEqual(post.Remarks, get.Remarks);
      Assert.AreEqual(post.Username, get.Username);
      // Password can not be retrieved. We also do not check additional fields such as LastErrorAt
    }

    [TestMethod]
    public async Task CreateNode_WrongIdSyntax_ShouldReturnBadREquest()
    {
      //arrange
      var create = new NodeViewModelCreate
      {
        Id = "some.host2", // missing port
        Remarks = "Some remarks2",
        Password = "somePassword2",
        Username = "user2"
      };
      var content = new StringContent(JsonSerializer.Serialize(create), Encoding.UTF8, "application/json");

      //act
      var (_, responseContent) = await Post<string>(UrlForKey(""), client, content, HttpStatusCode.BadRequest);
      var responseAsString = await responseContent.Content.ReadAsStringAsync();

      var vpd = JsonSerializer.Deserialize<ValidationProblemDetails>(responseAsString);

      Assert.AreEqual(1, vpd.Errors.Count());
      Assert.AreEqual("Id", vpd.Errors.First().Key);
    }

    [TestMethod]
    public async Task CreateNode_WrongIdSyntax2_ShouldReturnBadREquest()
    {
      //arrange
      var create = new NodeViewModelCreate
      {
        Id = "some.host2:abs", // not a port number
        Remarks = "Some remarks2",
        Password = "somePassword2",
        Username = "user2"
      };
      var content = new StringContent(JsonSerializer.Serialize(create), Encoding.UTF8, "application/json");

      //act
      var (_, responseContent) = await Post<string>(UrlForKey(""), client, content, HttpStatusCode.BadRequest);
      var responseAsString = await responseContent.Content.ReadAsStringAsync();

      var vpd = JsonSerializer.Deserialize<ValidationProblemDetails>(responseAsString);
      Assert.AreEqual(1, vpd.Errors.Count());
      Assert.AreEqual("Id", vpd.Errors.First().Key);
    }

    [TestMethod]
    public async Task CreateNode_NoUsername_ShouldReturnBadRequest()
    {
      //arrange
      var create = new NodeViewModelCreate
      {
        Id = "some.host2:2",
        Remarks = "Some remarks2",
        Password = "somePassword2",
        Username = null // missing username
      };
      var content = new StringContent(JsonSerializer.Serialize(create), Encoding.UTF8, "application/json");

      //act
      var (_, responseContent) = await Post<string>(UrlForKey(""), client, content, HttpStatusCode.BadRequest);

      var responseAsString =await responseContent.Content.ReadAsStringAsync();
      var vpd = JsonSerializer.Deserialize<ValidationProblemDetails>(responseAsString);
      Assert.AreEqual(1, vpd.Errors.Count());
      Assert.AreEqual("Username", vpd.Errors.First().Key);
    }
    
  }
}
