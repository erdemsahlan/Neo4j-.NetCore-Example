﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MoviesDotNetCore.Model;
using MoviesDotNetCore.Repositories;

namespace MoviesDotNetCore.Controllers;

[ApiController]
[Route("movie")]
public class MoviesController : ControllerBase
{
    private readonly IMovieRepository _movieRepository;

    public MoviesController(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    [Route("{title}")]
    [HttpGet]
    public Task<Movie> GetMovieDetails([FromRoute] string title)
    {
        return _movieRepository.FindByTitle(title);
    }
}