(function () {
    'use strict';

    var container = document.getElementById('swagger-ui');
    if (!container || typeof window.SwaggerUIBundle !== 'function') {
        return;
    }

    window.SwaggerUIBundle({
        url: container.getAttribute('data-spec-url'),
        domNode: container,
        deepLinking: true,
        tryItOutEnabled: true,
        displayRequestDuration: true,
        defaultModelsExpandDepth: 1,
        persistAuthorization: false,
        // queryConfigEnabled:false ignora ?url= y ?configUrl= (CVE-2018-25031); el JWT no se persiste en localStorage.
        queryConfigEnabled: false,
        validatorUrl: null
    });
}());
