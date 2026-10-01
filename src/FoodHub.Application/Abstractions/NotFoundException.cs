using System;
using System.Collections.Generic;
using System.Text;

namespace FoodHub.Application.Abstractions;

public sealed class NotFoundException(string message) : Exception(message);
