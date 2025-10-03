function speak(text) {
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'pt-BR';
    speechSynthesis.speak(utterance);
}

function loadSpeechScript() {
    console.log("Speech script ready");
}
