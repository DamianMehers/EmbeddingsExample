Example code that goes with my blog posts on
* [Adding extensions to sqlite-net](https://damian.fyi/xamarin/2025/04/19/adding-extensions-to-sqlite-net.html)
* [Getting, storing, and using LLM embeddings in a .NET Console App](https://damian.fyi/xamarin/2025/04/19/getting-storing-and-using-embeddings-in-dotnet)

To run it, install [ollama](https://ollama.com/) and install the [nomic](https://ollama.com/library/nomic-embed-text) model using `ollama pull nomic-embed-text`

Clone this project then:

```sh
cd EmbeddingsExample/EmbeddingsExample
dotnet run
```

You should see:

```
dotnet run
0 Generating embedding for Gold Discovered in California!
1 Generating embedding for The Great Fire of London Bridge
2 Generating embedding for First Steam-Powered Train Debuts
3 Generating embedding for The Telegraph Revolutionizes Communication
4 Generating embedding for The First Telephone Call by Alexander Graham Bell
5 Generating embedding for The Irish Potato Famine Strikes
6 Generating embedding for Darwin Publishes 'On the Origin of Species'
7 Generating embedding for The American Civil War Begins
8 Generating embedding for Eiffel Tower Construction Announced
9 Generating embedding for The Light Bulb Invented by Edison
Searching ...
Found: The Irish Potato Famine Strikes
Found: The Great Fire of London Bridge
Found: The Light Bulb Invented by Edison

```
