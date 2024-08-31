echo 'start move dockerfile...'
mv WebApi8Guide/Dockerfile ./
echo 'start build image...'
echo 'Check if image exists...'
if ! docker images | grep -q 'dotnet8dockerfile'; then
    echo 'Image not found, building...'
    docker build -t dotnet8dockerfile .
else
    echo 'Image already exists, skipping build.'
fi
echo 'start build container...'
echo 'Check if container exists...'
if docker ps -a --format '{{.Names}}' | grep -q 'testapp'; then
    echo 'Container exists, stopping and removing...'
    docker stop testapp
    docker rm testapp
fi
docker run -d -p 8080:8080 --name testapp dotnet8dockerfile:latest