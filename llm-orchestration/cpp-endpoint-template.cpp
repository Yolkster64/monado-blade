// C++ LLM API Endpoint Template
#include <cpprest/http_listener.h>
#include <cpprest/json.h>
using namespace web;
using namespace http;
using namespace http::experimental::listener;

void handle_post(http_request request)
{
    request.extract_json().then([=](json::value jvalue) {
        auto prompt = jvalue[U("prompt")].as_string();
        // Call your LLM backend here
        json::value response;
        response[U("result")] = json::value::string(U("<LLM output here>"));
        request.reply(status_codes::OK, response);
    });
}

int main()
{
    http_listener listener(U("http://localhost:8080/generate"));
    listener.support(methods::POST, handle_post);
    listener.open().wait();
    while (true); // Keep server running
    return 0;
}
