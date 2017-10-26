dotnet restore
dotnet build
cd GreetingsAll
rm -rf out
dotnet publish -c Release -o out
cd ..