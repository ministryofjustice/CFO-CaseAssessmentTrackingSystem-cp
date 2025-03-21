# Use runtime-only image (No SDK needed)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Create a non-root user and group
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

WORKDIR /app

# Copy already built files from the GitHub Actions pipeline
COPY ./publish ./ 

# Set permissions
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

ENTRYPOINT ["dotnet", "Cfo.Cats.Server.UI.dll"]
