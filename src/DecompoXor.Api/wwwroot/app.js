const conversation = document.querySelector("#conversation");
const form = document.querySelector("#composer-form");
const input = document.querySelector("#criteria-input");
const sendButton = document.querySelector("#send-button");
const newAnalysisButton = document.querySelector("#new-analysis");
const characterCount = document.querySelector("#character-count");
const liveStatus = document.querySelector("#live-status");
const welcomeTemplate = document.querySelector("#welcome-template");
const loadingSteps = ["Retrieving related context", "Building the task breakdown", "Validating the estimate"];

let isSubmitting = false;
let lastCriteria = "";

function makeElement(tag, className, text) {
  const element = document.createElement(tag);
  if (className) element.className = className;
  if (text !== undefined) element.textContent = text;
  return element;
}

function scrollToLatest() {
  conversation.scrollTo({ top: conversation.scrollHeight, behavior: "smooth" });
}

function appendUserMessage(criteria) {
  const message = makeElement("article", "message user-message enter");
  message.setAttribute("aria-label", "Your acceptance criteria");
  message.append(makeElement("div", "user-bubble", criteria));
  conversation.append(message);
  scrollToLatest();
}

function appendLoadingMessage() {
  const message = makeElement("article", "message assistant-message enter");
  message.setAttribute("aria-label", "Generating story decomposition");
  message.append(makeElement("div", "assistant-mark", "DX"));

  const content = makeElement("div", "assistant-content");
  content.append(makeElement("p", "assistant-label", "RAG analysis"));

  const panel = makeElement("div", "loading-panel");
  const copy = makeElement("div", "loading-copy");
  copy.append(makeElement("span", "loading-title", "Building your decomposition"));
  const stepLabel = makeElement("span", "loading-step", loadingSteps[0]);
  stepLabel.id = "loading-step-label";
  copy.append(stepLabel);
  panel.append(copy);

  const dots = makeElement("span", "loading-dots");
  dots.setAttribute("aria-hidden", "true");
  for (let index = 0; index < 3; index++) dots.append(makeElement("span", ""));
  panel.append(dots);

  content.append(panel);
  message.append(content);
  conversation.append(message);
  scrollToLatest();

  let step = 0;
  const timer = window.setInterval(() => {
    step = (step + 1) % loadingSteps.length;
    stepLabel.textContent = loadingSteps[step];
  }, 1900);

  return {
    element: message,
    stop() {
      window.clearInterval(timer);
    }
  };
}

function appendSection(parent, title, delay) {
  const section = makeElement("section", "result-section enter");
  section.style.setProperty("--delay", `${delay}ms`);
  section.append(makeElement("h3", "", title));
  parent.append(section);
  return section;
}

function appendError(messageText, retryCriteria) {
  const message = makeElement("article", "message assistant-message enter");
  message.append(makeElement("div", "assistant-mark", "DX"));
  const content = makeElement("div", "assistant-content");
  content.append(makeElement("p", "assistant-label", "Unable to complete analysis"));
  content.append(makeElement("div", "empty-result", messageText));

  if (retryCriteria) {
    const retry = makeElement("button", "copy-button", "Retry");
    retry.type = "button";
    retry.addEventListener("click", () => submitCriteria(retryCriteria));
    content.append(retry);
  }

  message.append(content);
  conversation.append(message);
  scrollToLatest();
}

function appendResult(result) {
  const message = makeElement("article", "message assistant-message enter");
  message.setAttribute("aria-label", "Story decomposition result");
  message.append(makeElement("div", "assistant-mark", "DX"));

  const content = makeElement("div", "assistant-content");
  content.append(makeElement("p", "assistant-label", "Story decomposition"));

  const toolbar = makeElement("div", "result-toolbar");
  const estimate = makeElement("div", "estimate-block");
  estimate.append(makeElement("span", "estimate-number", String(result.estimatedTotalStoryPoints ?? "-")));
  estimate.append(makeElement("span", "estimate-caption", "story points"));
  toolbar.append(estimate);

  const copyButton = makeElement("button", "copy-button", "Copy JSON");
  copyButton.type = "button";
  copyButton.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(JSON.stringify(result, null, 2));
      copyButton.textContent = "Copied";
      liveStatus.textContent = "Result copied as JSON.";
      window.setTimeout(() => { copyButton.textContent = "Copy JSON"; }, 1600);
    } catch {
      liveStatus.textContent = "Clipboard access is unavailable in this browser.";
    }
  });
  toolbar.append(copyButton);
  content.append(toolbar);

  const tasksSection = appendSection(content, "Tasks", 40);
  const tasks = Array.isArray(result.tasks) ? result.tasks : [];
  if (tasks.length === 0) {
    tasksSection.append(makeElement("div", "empty-result", "The response contained no tasks. Try again or refine the acceptance criteria."));
  } else {
    const list = makeElement("div", "task-list");
    tasks.forEach((task, index) => {
      const item = makeElement("article", "task-item");
      item.style.setProperty("--delay", `${60 + index * 45}ms`);
      item.classList.add("enter");
      item.append(makeElement("h4", "task-title", task.title || `Task ${index + 1}`));
      if (task.areaOfChange) item.append(makeElement("span", "area-tag", task.areaOfChange));
      item.append(makeElement("p", "task-description", task.description || ""));
      list.append(item);
    });
    tasksSection.append(list);
  }

  const questions = Array.isArray(result.questions) ? result.questions : [];
  if (questions.length > 0) {
    const section = appendSection(content, "Questions to resolve", 170);
    const list = makeElement("ol", "question-list");
    questions.forEach((question) => list.append(makeElement("li", "", question)));
    section.append(list);
  }

  const reasoningItems = normalizeReasoning(result.reasoning);
  if (reasoningItems.length > 0) {
    const section = appendSection(content, "Reasoning", 220);
    const list = makeElement("ul", "reasoning-list");
    reasoningItems.forEach((item) => list.append(makeElement("li", "", item)));
    section.append(list);
  }

  message.append(content);
  conversation.append(message);
  scrollToLatest();
}

function normalizeReasoning(reasoning) {
  if (Array.isArray(reasoning)) return reasoning.map(String).filter(Boolean);
  if (reasoning && typeof reasoning === "object") {
    return Object.entries(reasoning)
      .filter(([, value]) => value !== null && value !== undefined && String(value).trim())
      .map(([key, value]) => `${key}: ${value}`);
  }
  return reasoning ? [String(reasoning)] : [];
}

async function submitCriteria(criteria) {
  if (isSubmitting || !criteria.trim()) return;

  lastCriteria = criteria.trim();
  isSubmitting = true;
  setBusy(true);
  conversation.querySelector(".welcome")?.remove();
  appendUserMessage(lastCriteria);
  const loading = appendLoadingMessage();
  liveStatus.textContent = "Sending acceptance criteria for analysis.";

  try {
    const response = await fetch("/api/story/decompose", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ acceptanceCriteria: lastCriteria })
    });

    let result;
    try {
      result = await response.json();
    } catch {
      throw new Error(`The server returned an unreadable response (HTTP ${response.status}).`);
    }

    if (!response.ok) {
      throw new Error(result.detail || result.title || `Request failed (HTTP ${response.status}).`);
    }

    loading.stop();
    loading.element.remove();
    appendResult(result);
    liveStatus.textContent = "Story decomposition complete.";
  } catch (error) {
    loading.stop();
    loading.element.remove();
    appendError(error instanceof Error ? error.message : "An unexpected error occurred.", lastCriteria);
    liveStatus.textContent = "Story decomposition failed.";
  } finally {
    isSubmitting = false;
    setBusy(false);
    input.value = "";
    updateCharacterCount();
    input.focus();
  }
}

function setBusy(busy) {
  input.disabled = busy;
  sendButton.disabled = busy;
  newAnalysisButton.disabled = busy;
  sendButton.querySelector("span:first-child").textContent = busy ? "Working" : "Decompose";
}

function updateCharacterCount() {
  characterCount.textContent = `${input.value.length} / 6000`;
}

function resetConversation() {
  if (isSubmitting) return;
  conversation.replaceChildren(welcomeTemplate.content.firstElementChild.cloneNode(true));
  input.value = "";
  lastCriteria = "";
  updateCharacterCount();
  input.focus();
}

conversation.replaceChildren(welcomeTemplate.content.firstElementChild.cloneNode(true));
conversation.addEventListener("click", (event) => {
  const sampleButton = event.target.closest("[data-prompt]");
  if (!sampleButton) return;
  input.value = sampleButton.dataset.prompt;
  updateCharacterCount();
  input.focus();
});

form.addEventListener("submit", (event) => {
  event.preventDefault();
  submitCriteria(input.value);
});

input.addEventListener("input", updateCharacterCount);
input.addEventListener("keydown", (event) => {
  if (event.key === "Enter" && !event.shiftKey) {
    event.preventDefault();
    form.requestSubmit();
  }
});

newAnalysisButton.addEventListener("click", resetConversation);