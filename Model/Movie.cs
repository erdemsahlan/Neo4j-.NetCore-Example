﻿using System.Collections.Generic;

namespace MoviesDotNetCore.Model;

public record Movie(string? Title= null, IEnumerable<Person>? Cast = null, long? Released = null, string? Tagline = null,
    long? Votes = null);