const questions = [
  {
    category: "기초",
    text: "이터널 리턴의 대표 전장은 어디일까요?",
    answers: ["루미아 섬", "소환사의 협곡", "에란겔", "할로우 네스트"],
    correct: 0,
    note: "이터널 리턴의 주요 배경은 루미아 섬입니다.",
  },
  {
    category: "기초",
    text: "이터널 리턴의 장르 조합으로 가장 가까운 것은?",
    answers: ["배틀로얄 + MOBA", "턴제 RPG + 리듬게임", "농장 경영 + 퍼즐", "비주얼 노벨 + 레이싱"],
    correct: 0,
    note: "실시간 교전, 파밍, 제작, 생존이 섞인 배틀로얄 MOBA에 가깝습니다.",
  },
  {
    category: "캐릭터",
    text: "전기톱을 상징 무기로 떠올리기 쉬운 실험체는?",
    answers: ["재키", "아야", "피오라", "키아라"],
    correct: 0,
    note: "재키는 이터널 리턴 초창기부터 강렬한 전기톱 이미지로 알려져 있습니다.",
  },
  {
    category: "캐릭터",
    text: "권총, 돌격소총, 저격총 같은 총기 이미지가 강한 실험체는?",
    answers: ["아야", "매그너스", "레온", "쇼우"],
    correct: 0,
    note: "아야는 총기류를 다루는 대표적인 실험체입니다.",
  },
  {
    category: "운영",
    text: "초반 파밍에서 가장 중요한 판단은?",
    answers: ["루트에 맞춰 재료를 빠르게 모은다", "아무 지역에서 춤만 춘다", "무조건 야생동물을 피한다", "장비를 만들지 않는다"],
    correct: 0,
    note: "계획한 루트대로 핵심 재료를 모아 장비 타이밍을 당기는 것이 중요합니다.",
  },
  {
    category: "운영",
    text: "금지 구역 타이머를 놓치면 생기는 가장 큰 문제는?",
    answers: ["이동 선택지가 줄고 탈락 위험이 커진다", "상점 가격이 오른다", "캐릭터 이름이 바뀐다", "스킬 아이콘이 사라진다"],
    correct: 0,
    note: "금지 구역은 동선을 압박하므로 맵과 시간을 계속 봐야 합니다.",
  },
  {
    category: "전투",
    text: "교전 직전 확인하면 좋은 정보가 아닌 것은?",
    answers: ["상대 장비와 체력", "내 스킬 쿨타임", "주변 시야와 퇴로", "모니터 브랜드"],
    correct: 3,
    note: "장비, 쿨타임, 시야, 퇴로가 교전 판단의 핵심입니다.",
  },
  {
    category: "전투",
    text: "스킬을 모두 쏟은 직후 가장 조심해야 할 상황은?",
    answers: ["반격 타이밍", "닉네임 색상", "로비 배경", "튜토리얼 문구"],
    correct: 0,
    note: "핵심 스킬이 빠진 순간에는 상대가 역으로 들어오기 쉽습니다.",
  },
  {
    category: "오브젝트",
    text: "희귀 재료와 강한 보상을 두고 교전이 자주 일어나는 이유는?",
    answers: ["장비 격차를 만들 수 있어서", "경험치를 모두 잃어서", "맵이 완전히 초기화돼서", "채팅이 잠겨서"],
    correct: 0,
    note: "중요 오브젝트는 팀의 파워 타이밍을 크게 앞당깁니다.",
  },
  {
    category: "오브젝트",
    text: "위클라인을 두고 팀들이 모이는 가장 큰 이유는?",
    answers: ["강력한 전투 보상", "캐릭터 생성권", "영구 스킨 교환", "튜토리얼 스킵"],
    correct: 0,
    note: "위클라인은 경기 후반 주도권과 연결되는 큰 보상입니다.",
  },
  {
    category: "팀플",
    text: "스쿼드에서 좋은 콜에 가까운 것은?",
    answers: ["궁극기 없음, 5초 뒤 빠지자", "아무튼 싸워", "나만 믿고 흩어져", "지도 안 봄"],
    correct: 0,
    note: "쿨타임, 위치, 타이밍처럼 실행 가능한 정보가 좋은 콜입니다.",
  },
  {
    category: "팀플",
    text: "팀원이 파밍 루트에서 늦어졌을 때 무난한 선택은?",
    answers: ["합류 가능한 지점과 시간을 맞춘다", "혼자 끝까지 깊게 들어간다", "아이템을 전부 버린다", "맵을 보지 않는다"],
    correct: 0,
    note: "템포가 갈렸다면 합류 지점을 다시 잡는 것이 안정적입니다.",
  },
];

const categories = ["전체", ...new Set(questions.map((question) => question.category))];
let activeCategory = "전체";
let deck = [];
let index = 0;
let score = 0;
let combo = 0;
let wrongAnswers = [];
let locked = false;
let timeLeft = 20;
let timerId = null;

const scoreEl = document.querySelector("#score");
const comboEl = document.querySelector("#combo");
const timerEl = document.querySelector("#timer");
const countEl = document.querySelector("#question-count");
const progressEl = document.querySelector("#progress-bar");
const categoryEl = document.querySelector("#question-category");
const questionEl = document.querySelector("#question-text");
const answersEl = document.querySelector("#answers");
const feedbackEl = document.querySelector("#feedback");
const nextButton = document.querySelector("#next-button");
const categoryRow = document.querySelector("#category-row");
const reviewList = document.querySelector("#review-list");
const restartButton = document.querySelector("#restart-button");
const rankTitle = document.querySelector("#rank-title");
const rankCopy = document.querySelector("#rank-copy");
const canvas = document.querySelector("#island-radar");
const ctx = canvas.getContext("2d");

function shuffle(items) {
  return [...items].sort(() => Math.random() - 0.5);
}

function buildCategories() {
  categoryRow.innerHTML = "";
  categories.forEach((category) => {
    const button = document.createElement("button");
    button.type = "button";
    button.textContent = category;
    button.className = category === activeCategory ? "active" : "";
    button.addEventListener("click", () => {
      activeCategory = category;
      startGame();
    });
    categoryRow.append(button);
  });
}

function startGame() {
  const pool =
    activeCategory === "전체"
      ? questions
      : questions.filter((question) => question.category === activeCategory);
  deck = shuffle(pool).slice(0, Math.min(10, pool.length));
  index = 0;
  score = 0;
  combo = 0;
  wrongAnswers = [];
  locked = false;
  nextButton.disabled = true;
  buildCategories();
  renderReview();
  renderQuestion();
  drawRadar();
}

function renderQuestion() {
  clearInterval(timerId);
  locked = false;
  timeLeft = 20;
  const current = deck[index];
  categoryEl.textContent = current.category;
  questionEl.textContent = current.text;
  feedbackEl.textContent = "정답을 고르면 바로 판정됩니다.";
  answersEl.innerHTML = "";
  nextButton.disabled = true;
  nextButton.textContent = index === deck.length - 1 ? "결과 보기" : "다음 문제";

  current.answers.forEach((answer, answerIndex) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "answer-button";
    button.textContent = answer;
    button.addEventListener("click", () => chooseAnswer(answerIndex));
    answersEl.append(button);
  });

  syncStatus();
  timerId = setInterval(tick, 1000);
}

function chooseAnswer(answerIndex) {
  if (locked) return;
  locked = true;
  clearInterval(timerId);

  const current = deck[index];
  const buttons = [...document.querySelectorAll(".answer-button")];
  buttons.forEach((button, buttonIndex) => {
    button.disabled = true;
    if (buttonIndex === current.correct) button.classList.add("correct");
    if (buttonIndex === answerIndex && answerIndex !== current.correct) button.classList.add("wrong");
  });

  if (answerIndex === current.correct) {
    combo += 1;
    score += 100 + combo * 15 + timeLeft * 2;
    feedbackEl.textContent = `정답! ${current.note}`;
  } else {
    combo = 0;
    wrongAnswers.push({
      text: current.text,
      answer: current.answers[current.correct],
      note: current.note,
    });
    feedbackEl.textContent = `아쉽다. 정답은 "${current.answers[current.correct]}"입니다. ${current.note}`;
  }

  nextButton.disabled = false;
  syncStatus();
  renderReview();
  drawRadar();
}

function tick() {
  timeLeft -= 1;
  timerEl.textContent = timeLeft;
  if (timeLeft <= 0) {
    chooseAnswer(-1);
  }
}

function nextQuestion() {
  if (index < deck.length - 1) {
    index += 1;
    renderQuestion();
    return;
  }
  showResult();
}

function showResult() {
  clearInterval(timerId);
  locked = true;
  const accuracy = Math.round(((deck.length - wrongAnswers.length) / deck.length) * 100);
  categoryEl.textContent = "결과";
  questionEl.textContent = `최종 점수 ${score}점, 정답률 ${accuracy}%`;
  answersEl.innerHTML = "";
  feedbackEl.textContent =
    wrongAnswers.length === 0
      ? "완벽합니다. 루미아 섬 들어가도 감각 살아있겠는데요."
      : "오답 복습에서 놓친 포인트만 다시 보면 다음 판은 더 단단해집니다.";
  nextButton.disabled = true;
  updateRank();
  drawRadar(true);
}

function syncStatus() {
  scoreEl.textContent = score;
  comboEl.textContent = combo;
  timerEl.textContent = timeLeft;
  countEl.textContent = `${Math.min(index + 1, deck.length)} / ${deck.length}`;
  progressEl.style.width = `${((index + Number(locked)) / deck.length) * 100}%`;
  updateRank();
}

function updateRank() {
  const solved = Math.max(1, index + Number(locked));
  const accuracy = (solved - wrongAnswers.length) / solved;
  if (accuracy >= 0.9 && score > 850) {
    rankTitle.textContent = "루미아 분석관";
    rankCopy.textContent = "판단 속도와 기본기가 모두 안정적입니다.";
  } else if (accuracy >= 0.7) {
    rankTitle.textContent = "숙련 생존자";
    rankCopy.textContent = "운영 감각이 좋습니다. 오브젝트 타이밍만 더 챙기면 됩니다.";
  } else if (accuracy >= 0.45) {
    rankTitle.textContent = "파밍 중인 실험체";
    rankCopy.textContent = "기본기는 잡혀갑니다. 오답을 눌러 담으면 금방 좋아져요.";
  } else {
    rankTitle.textContent = "루미아 입문자";
    rankCopy.textContent = "천천히 감을 잡는 중입니다. 지금은 루트 외우는 단계.";
  }
}

function renderReview() {
  reviewList.innerHTML = "";
  if (wrongAnswers.length === 0) {
    const item = document.createElement("li");
    item.innerHTML = "<strong>아직 오답 없음</strong>틀린 문제가 생기면 여기에 쌓입니다.";
    reviewList.append(item);
    return;
  }

  wrongAnswers.slice(-4).forEach((wrong) => {
    const item = document.createElement("li");
    item.innerHTML = `<strong>${wrong.text}</strong>정답: ${wrong.answer}<br>${wrong.note}`;
    reviewList.append(item);
  });
}

function drawRadar(finalPulse = false) {
  const width = canvas.width;
  const height = canvas.height;
  const centerX = width / 2;
  const centerY = height / 2;
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = "#0e141d";
  ctx.fillRect(0, 0, width, height);

  ctx.strokeStyle = "rgba(97, 216, 255, 0.18)";
  ctx.lineWidth = 1;
  for (let radius = 38; radius < 150; radius += 34) {
    ctx.beginPath();
    ctx.arc(centerX, centerY, radius, 0, Math.PI * 2);
    ctx.stroke();
  }

  const accuracy = deck.length ? (Math.max(0, index + Number(locked) - wrongAnswers.length) / Math.max(1, index + Number(locked))) : 0;
  const pulse = finalPulse ? 1 : 0.55 + accuracy * 0.45;
  const points = [
    [centerX, centerY - 98 * pulse],
    [centerX + 96 * pulse, centerY - 18],
    [centerX + 58, centerY + 86 * pulse],
    [centerX - 70 * pulse, centerY + 74],
    [centerX - 104, centerY - 22 * pulse],
  ];

  ctx.fillStyle = "rgba(94, 224, 160, 0.22)";
  ctx.strokeStyle = "#5ee0a0";
  ctx.lineWidth = 2;
  ctx.beginPath();
  points.forEach(([x, y], pointIndex) => {
    if (pointIndex === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.closePath();
  ctx.fill();
  ctx.stroke();

  ctx.fillStyle = "#ffd166";
  points.forEach(([x, y]) => {
    ctx.beginPath();
    ctx.arc(x, y, 4, 0, Math.PI * 2);
    ctx.fill();
  });

  ctx.fillStyle = "rgba(244, 247, 251, 0.74)";
  ctx.font = "16px GalmuriMono9, sans-serif";
  ctx.fillText("ROUTE", 24, 38);
  ctx.fillText("OBJECT", 244, 58);
  ctx.fillText("FIGHT", 258, 236);
  ctx.fillText("CALL", 32, 230);
}

nextButton.addEventListener("click", nextQuestion);
restartButton.addEventListener("click", startGame);
startGame();
