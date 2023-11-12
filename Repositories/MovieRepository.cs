using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoviesDotNetCore.Model;
using Neo4j.Driver;

namespace MoviesDotNetCore.Repositories;

public interface IMovieRepository
{
    Task<Movie> FindByTitle(string title);
    Task<List<Movie>> Search(string search);
}

public class MovieRepository : IMovieRepository
{
    private readonly IDriver _driver;

    public MovieRepository(IDriver driver)
    {
        _driver = driver;
    }

    public async Task<Movie> FindByTitle(string title)
    {
        if (title == "favicon.ico")
            return null;
        
        await using var session = _driver.AsyncSession(WithDatabase);

        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(@"
                        MATCH (movie:Movie {title:$title})
                        OPTIONAL MATCH (movie)<-[r]-(person:Person)
                        RETURN movie.title AS title,
                               collect({
                                   name:person.name,
                                   job: head(split(toLower(type(r)),'_')),
                                   role: reduce(acc = '', role IN r.roles | acc + CASE WHEN acc='' THEN '' ELSE ', ' END + role)}
                               ) AS cast",
                new {title}
            );
            
            return await cursor.SingleAsync(record => new Movie(
                record["title"].As<string>(),
                MapCast(record["cast"].As<List<IDictionary<string, object>>>())
            ));
        });
    }

    public async Task<List<Movie>> Search(string search)
    {
        await using var session = _driver.AsyncSession(WithDatabase);
        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(@"
                        MATCH (movie:Movie)
                        WHERE toLower(movie.title) CONTAINS toLower($title)
                        RETURN movie.title AS title,
                               movie.released AS released,
                               movie.tagline AS tagline,
                               movie.votes AS votes",
                new {title = search}
            );

            return await cursor.ToListAsync(record => new Movie(
                record["title"].As<string>(),
                Tagline: record["tagline"].As<string>(),
                Released: record["released"].As<long>(),
                Votes: record["votes"]?.As<long>()
            ));
        });
    }
    private static IEnumerable<Person> MapCast(IEnumerable<IDictionary<string, object>> persons)
    {
        return persons
            .Select(dictionary =>
                new Person(
                    dictionary["name"].As<string>(),
                    dictionary["job"].As<string>(),
                    dictionary["role"].As<string>()
                )
            ).ToList();
    }

    private static void WithDatabase(SessionConfigBuilder sessionConfigBuilder)
    {
        var neo4jVersion = Environment.GetEnvironmentVariable("NEO4J_VERSION") ?? "";
        if (!neo4jVersion.StartsWith("4"))
            return;

        sessionConfigBuilder.WithDatabase(Database());
    }

    private static string Database()
    {
        return Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "movies";
    }
}