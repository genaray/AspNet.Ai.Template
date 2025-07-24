<script>
    // --- Imports ---
    import { onMount } from 'svelte';
    import { Card, CardHeader, CardTitle, CardContent } from '$lib/components/ui/card';
    import { Input } from '$lib/components/ui/input';
    import { Button } from '$lib/components/ui/button';
    import { Loader2 } from 'lucide-svelte';

    // --- State variables ---
    let prompt = '';
    let messages = [];
    let isLoading = false;
    let typingDots = 0;
    let typingInterval;
    let animatedBotText = '';
    let animating = false;

    // --- Example intro message (always first) ---
    const introMessage = {
        text: `<b>Welcome!</b> </br> This is a showcase of a LLM that is integrated into the backend using MCP + LangChain to call up business logic and support the end user. </br> <b>Here are some example prompts you can try:</b><ul class='list-disc list-inside mt-2'><li>What properties does the user have?</li><li>What actions can you provide?</li><li>Change the name of the user with the id 123 to Justin.</li></ul>`,
        sender: 'bot',
        isIntro: true
    };

    // Svelte action to set innerHTML safely
    export function html(node) {
        node.innerHTML = node.textContent;
    }

    // --- Send user message and handle bot response ---
    async function sendMessage() {

        if (!prompt.trim() || isLoading) return;

        const userMessage = { text: prompt, sender: 'user' };
        messages = [...messages, userMessage];
        isLoading = true;
        const currentPrompt = prompt;
        prompt = '';
        animatedBotText = '';
        animating = false;

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
            isLoading = false; // Hide Typing... immediately before animating response

            await animateBotResponse(rawResponse);
        } catch (error) {

            isLoading = false; // Hide Typing... immediately on error
            console.error('Error sending message:', error);
            await animateBotResponse('Error: Could not connect to the LangChain service.');
        }
    }

    // --- Animate bot response character by character, we cant use $ statements here since its async ---
    async function animateBotResponse(text) {

        animating = true;
        animatedBotText = '';

        for (let i = 0; i < text.length; i++) {

            animatedBotText += text[i];
            await new Promise(r => setTimeout(r, 15));
        }

        messages = [...messages, { text, sender: 'bot' }];
        animating = false;
        animatedBotText = '';
    }

    // --- Typing dots animation for 'Typing...' indicator, reactive function, called once isLoading changes ---
    $: if (isLoading) {

        if (!typingInterval) {

            typingInterval = setInterval(() => {
                typingDots = (typingDots + 1) % 4;
            }, 400);
        }
    } else {

        clearInterval(typingInterval);
        typingInterval = null;
        typingDots = 0;
    }

    // --- Scroll chat to bottom on new message or animation ---
    function scrollToBottom() {

        const chatWindow = document.getElementById('chat-window');
        if (chatWindow) {

            chatWindow.scrollTop = chatWindow.scrollHeight;
        }
    }

    // --- Scroll to bottom on mount and on new message or animation ---
    onMount(scrollToBottom);
    $: messages, scrollToBottom();
    $: animatedBotText, scrollToBottom();
</script>

<main class="min-h-screen bg-background flex items-center justify-center">
    <div class="w-full max-w-4xl mx-auto flex flex-col opacity-0 animate-fade-in">

        <!-- Chat Section -->
        <Card class="w-full flex flex-col h-[80vh]">
            <!-- Card header with fade-in delay -->
            <div class="opacity-0 animate-fade-in delay-100">
                <CardHeader class="text-center">
                    <CardTitle class="text-3xl font-bold text-gray-800">LangChain Chat</CardTitle>
                </CardHeader>
            </div>

            <!-- Card content (chat and input) with fade-in delay -->
            <div class="opacity-0 animate-fade-in delay-200 flex-1 flex flex-col p-0 mt-2">
                <CardContent class="flex-1 flex flex-col p-0">

                    <!-- Messages Window -->
                    <div id="chat-window" class="flex-1 overflow-y-auto border border-gray-300 rounded-lg p-4 mb-4 bg-gray-50">

                        <!-- Always show intro message first -->
                        <div class="mb-3 text-left">
                            <span class="inline-block px-4 py-2 rounded-lg bg-gray-300 text-gray-800">
                                {@html introMessage.text}
                            </span>
                        </div>

                        {#each messages as message}
                            <!-- User and bot messages -->
                            <div class="mb-3 {message.sender === 'user' ? 'text-right' : 'text-left'}">
                                <span class="inline-block px-4 py-2 rounded-lg {message.sender === 'user' ? 'bg-blue-500 text-white' : 'bg-gray-300 text-gray-800'}">
                                    {message.text}
                                </span>
                            </div>
                        {/each}
                        
                        {#if isLoading}
                            <!-- Animated 'Typing...' indicator while waiting for response -->
                            <div class="mb-3 text-left flex items-center gap-2">
                                <Loader2 class="h-5 w-5 animate-spin text-gray-500" />
                                <span class="inline-block px-4 py-2 rounded-lg bg-gray-300 text-gray-800">
                                    Typing{'.'.repeat(typingDots)}
                                </span>
                            </div>
                        {/if}

                        {#if animating && animatedBotText}
                            <!-- Animated bot response (character by character) -->
                            <div class="mb-3 text-left">
                                <span class="inline-block px-4 py-2 rounded-lg bg-gray-300 text-gray-800">
                                    {animatedBotText}
                                </span>
                            </div>
                        {/if}
                    </div>

                    <!-- Input Area -->
                    <form class="flex gap-2 mt-auto" on:submit|preventDefault={sendMessage}>
                        <Input
                            type="text"
                            bind:value={prompt}
                            placeholder="Type your message..."
                            class="flex-1"
                            disabled={isLoading || animating}
                            on:keypress={(e) => { if (e.key === 'Enter') sendMessage(); }}
                        />
                        <Button type="submit" disabled={isLoading || animating}>
                            Send
                        </Button>
                    </form>
                </CardContent>
            </div>
        </Card>
    </div>
</main>

<style>

    /* Fade-in animation, matching other pages */
    @keyframes fade-in {
        from {
            opacity: 0;
        }
        to {
            opacity: 1;
        }
    }

    .animate-fade-in {
        animation: fade-in 1s ease-out forwards;
    }

    .delay-100 {
        animation-delay: 0.2s;
    }

    .delay-200 {
        animation-delay: 0.4s;
    }

    .delay-300 {
        animation-delay: 0.6s;
    }
</style>
