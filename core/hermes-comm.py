# Hermes Cross-Language Communication (Python)
import socket

def send_message(target_host, target_port, message):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect((target_host, target_port))
        s.sendall(message.encode())
        return s.recv(1024).decode()

# Example usage: send_message('localhost', 9000, 'Hello Hermes!')
