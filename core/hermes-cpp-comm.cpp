// Hermes C++ Cross-Language Communication
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#pragma comment(lib, "ws2_32.lib")

int main() {
    WSADATA wsaData;
    WSAStartup(MAKEWORD(2,2), &wsaData);
    SOCKET s = socket(AF_INET, SOCK_STREAM, 0);
    sockaddr_in server;
    server.sin_family = AF_INET;
    server.sin_port = htons(9000);
    inet_pton(AF_INET, "127.0.0.1", &server.sin_addr);
    connect(s, (sockaddr*)&server, sizeof(server));
    const char* msg = "Hello from C++ Hermes!";
    send(s, msg, strlen(msg), 0);
    char buf[1024] = {0};
    recv(s, buf, 1024, 0);
    std::cout << "Received: " << buf << std::endl;
    closesocket(s);
    WSACleanup();
    return 0;
}
