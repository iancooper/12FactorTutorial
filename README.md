# Setup
You will need to have RabbitMQ installed to use this sample

# Restore
You will need to navigate to the project directory and use dotnet restore to pull the dependencies for this project
Alternatively you could use a Docker container to isolate the dependencies for these apps, and pull their dependencies when you build the container

cd .\GreetingsSender
dotnet restore

cd .\GreetingsReceiver
dotnet restore

# Run
Use dotnet run to execute both the recevier  and the sender
Create the consumer first as it will create the RabbitMQ queue to listen for events from the sender. If you run the sender without first running the receiver, then you will lost the message as it has no subscriber

cd .\GreetingsReceiver
dotnet run

cd .\GreetingsSender
dotnet run



