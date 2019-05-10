import React, { Component } from 'react';
import './App.css';
import axios from 'axios';
import { Input, Button } from 'semantic-ui-react';
import data from './data'

const BASE_URL = "http://localhost:60084/api/v1";

class App extends Component {
    state = { searchPhrase: '', results: [] }

    search = () => {
        axios.get(BASE_URL + '/search/' + this.state.searchPhrase)
        .then(response => this.setState({ results: response.data }))
        //this.setState({ results: data })
    }

    render() {
        return (
            <div className="App">
                <Input 
                    placeholder="Enter search phrase..."
                    onChange={(event, data) => this.setState({ searchPhrase: data.value })}
                />
                <Button
                    onClick={this.search}
                >
                    Search
                </Button>
                <div>
                    {this.state.results.filter(r => r.freeUploadSlots > 0).map(r =>
                        <ul>
                            <li>{r.username}; {r.freeUploadSlots}, {r.queueLength}</li>
                            <ul>
                                {r.files.map(f => 
                                    <li><a href={BASE_URL + '/download/' + r.username + '/' + encodeURIComponent(f.filename)}>{f.filename}</a></li>
                                )}
                            </ul>
                        </ul>
                    )}
                </div>
            </div>
        )
    }
}

export default App;
