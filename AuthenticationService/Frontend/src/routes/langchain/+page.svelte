<script>
    import { onMount } from 'svelte';

    let prompt = '';
    let messages = [];
    let isLoading = false;

    async function sendMessage() {
        if (!prompt.trim()) return;

        const userMessage = { text: prompt, sender: 'user' };
        messages = [...messages, userMessage];
        isLoading = true;
        const currentPrompt = prompt;
        prompt = ''; // Clear input immediately

        try {
            const response = await fetch('/api/UserServiceProxy/langChain', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(currentPrompt)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const rawResponse = await response.text();
            const botMessage = { text: rawResponse, sender: 'bot' };
            messages = [...messages, botMessage];
        } catch (error) {
            console.error('Error sending message:', error);
            const errorMessage = { text: 'Error: Could not connect to the LangChain service.', sender: 'bot' };
            messages = [...messages, errorMessage];
        } finally {
            isLoading = false;
        }
    }

    // Optional: Scroll to bottom of chat on new message
    onMount(() => {
        const chatWindow = document.getElementById('chat-window');
        if (chatWindow) {
            chatWindow.scrollTop = chatWindow.scrollHeight;
        }
    });

    // Reactive statement to scroll on message update
    $: messages, (() => {
        const chatWindow = document.getElementById('chat-window');
        if (chatWindow) {
            chatWindow.scrollTop = chatWindow.scrollHeight;
        }
    })();
</script>

<main class="min-h-screen bg-gradient-to-r from-blue-50 to-indigo-50 flex flex-col items-center justify-center py-10">
    <div class="w-full max-w-3xl mx-auto px-4 bg-white rounded-lg shadow-xl p-6 flex flex-col" style="height: 80vh;">
        <h1 class="text-3xl font-bold text-gray-800 mb-6 text-center">LangChain Chat</h1>

        <!-- Chat Window -->
        <div id="chat-window" class="flex-1 overflow-y-auto border border-gray-300 rounded-lg p-4 mb-4 bg-gray-50">
            {#each messages as message}
                <div class="mb-3 {message.sender === 'user' ? 'text-right' : 'text-left'}">
                    <span class="inline-block px-4 py-2 rounded-lg {message.sender === 'user' ? 'bg-blue-500 text-white' : 'bg-gray-300 text-gray-800'}">
                        {message.text}
                    </span>
                </div>
            {/each}
            {#if isLoading}
                <div class="mb-3 text-left">
                    <span class="inline-block px-4 py-2 rounded-lg bg-gray-300 text-gray-800">
                        Typing...
                    </span>
                </div>
            {/if}
        </div>

        <!-- Input Area -->
        <div class="flex">
            <input
                type="text"
                bind:value={prompt}
                on:keypress={(e) => { if (e.key === 'Enter') sendMessage(); }}
                placeholder="Type your message..."
                class="flex-1 p-3 border border-gray-300 rounded-l-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={isLoading}
            />
            <button
                on:click={sendMessage}
                class="px-6 py-3 bg-blue-600 text-white rounded-r-lg hover:bg-blue-700 transition transform hover:scale-105 focus:outline-none focus:ring-2 focus:ring-blue-500"
                disabled={isLoading}
            >
                Send
            </button>
        </div>
    </div>
</main>

<style>
    /* Add any specific styles here if needed, though Tailwind handles most */
</style>
