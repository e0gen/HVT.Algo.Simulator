namespace HVT.Core.Domain.Models;

public record Instrument(string Symbol, string Name, decimal TickSize, string Currency);