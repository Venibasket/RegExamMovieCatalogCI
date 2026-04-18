using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using RegExamMovieCatalog.Models;



namespace RegExamMovieCatalog
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";

        private const string LoginEmail = "hristov95@softuni.bg";
        private const string LoginPassword = "hristov123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(LoginEmail, LoginPassword);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            var newMovie = new MovieDTO
            {
                Title = "Test Movie",
                Description = "This is a test movie."
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(newMovie);
            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseData.Movie, Is.Not.Null, "Response should contain movie object.");
            var createdMovie = responseData.Movie;
            Assert.That(createdMovie.Id, Is.Not.Null.And.Not.Empty, "Created movie should have a valid Id.");
            Assert.That(responseData.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = createdMovie.Id;
        }

        [Order(2)]
        [Test]
        public void EditMovie_WithValidData_ShouldReturnSuccess()
        {
            var editedMovie = new MovieDTO
            {
                Title = "Edited Test Movie",
                Description = "This is an edited test movie."
            };
            var request = new RestRequest("/api/Movie/Edit/", Method.Put);

            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editedMovie);

            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseData.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnNonEmptyArray()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);
            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseData, Is.Not.Null.And.Not.Empty, "Expected non-empty array of movies.");
        }

        [Order(4)]
        [Test]
        public void DeleteMovie_WithValidId_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseData.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var newMovie = new MovieDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(newMovie);
            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Order(6)]
        [Test]
        public void EditMovie_WithInvalidId_ShouldReturnBadRequest()
        {
            var editedMovie = new MovieDTO
            {
                Title = "Edited Test Movie",
                Description = "This is an edited test movie."
            };
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", "12345");
            request.AddJsonBody(editedMovie);
            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(responseData.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteMovie_WithInvalidId_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", "12345");
            var response = this.client.Execute(request);

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(responseData.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }

}
