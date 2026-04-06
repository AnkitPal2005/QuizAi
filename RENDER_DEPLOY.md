# Render deployment

This project is ready to deploy on Render with Docker and Render Postgres.

## Files added

- `render.yaml`: Blueprint for one Docker web service and one Postgres database
- `.dockerignore`: Keeps Docker build context small

## Deploy steps

1. Push this repository to GitHub, GitLab, or Bitbucket.
2. Open Render.
3. Go to `Blueprints` and create a new Blueprint instance from this repo.
4. Render will create:
   - `aiquizplatform` web service
   - `aiquizplatform-db` Postgres database
5. Before the first successful deploy, fill these secret env vars in Render:
   - `Groq__ApiKey`
   - `Email__Username`
   - `Email__Password`
   - `Email__SenderEmail`

## Important config

- Database connection is supplied from Render Postgres through `ConnectionStrings__DefaultConnection`.
- The app automatically applies EF Core migrations on startup.
- The app now binds to Render's `PORT` environment variable.
- Reverse proxy headers are enabled so HTTPS works correctly behind Render's load balancer.

## Security note

Do not keep real API keys or SMTP passwords in local config files that might be committed later. Store them in Render environment variables only.
